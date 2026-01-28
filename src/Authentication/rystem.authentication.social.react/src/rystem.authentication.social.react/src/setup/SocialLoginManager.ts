import { ProviderType, SocialLoginSettings, SocialToken, Token } from "..";
import { PkceStorageService } from "../services/PkceStorageService";
import { TokenStorageService } from "../services/TokenStorageService";
import { UserStorageService } from "../services/UserStorageService";

export class SocialLoginManager {
    private static instance: SocialLoginManager | null;
    public settings: SocialLoginSettings;
    public refresher: () => void;
    private pkceStorage: PkceStorageService;
    private tokenStorage: TokenStorageService;
    private userStorage: UserStorageService;
    
    private constructor(settings: SocialLoginSettings | null) {
        this.settings = settings ?? {} as SocialLoginSettings;
        this.refresher = () => { };
        
        // Initialize storage services using the configured storage
        this.pkceStorage = new PkceStorageService(this.settings.storageService);
        this.tokenStorage = new TokenStorageService(this.settings.storageService);
        this.userStorage = new UserStorageService(this.settings.storageService);
    }
    
    public static Instance(settings: SocialLoginSettings | null): SocialLoginManager {
        if (SocialLoginManager.instance == null) {
            if (settings == null)
                settings = {} as SocialLoginSettings;
            SocialLoginManager.instance = new SocialLoginManager(settings);
            // No default redirectPath - only use if explicitly configured
        }
        return SocialLoginManager.instance;
    }
    
    public updateToken(provider: ProviderType, code: string): Promise<SocialToken> {
        const providerName = ProviderType[provider].toLowerCase();
        
        // Get code_verifier from PKCE storage (don't remove yet!)
        const codeVerifier = this.pkceStorage.getCodeVerifier(providerName);
        
        // Build query string with redirectPath only if explicitly configured in platform
        const redirectPathParam = (this.settings.platform?.redirectPath && this.settings.platform.redirectPath.trim()) 
            ? `&redirectPath=${encodeURIComponent(this.settings.platform.redirectPath)}` 
            : '';
        const queryString = `${this.settings.apiUri}/api/Authentication/Social/Token?provider=${provider}&code=${code}${redirectPathParam}`;
        
        let fetchPromise: Promise<Response>;
        
        if (codeVerifier) {
            // If code_verifier exists, send it in POST body
            fetchPromise = fetch(
                queryString,
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        code_verifier: codeVerifier
                    })
                }
            );
        } else {
            // Fallback: GET request without PKCE (backward compatibility)
            fetchPromise = fetch(queryString);
        }
        
        return fetchPromise
            .then(t => {
                if (!t.ok) {
                    throw new Error(`Token exchange failed: ${t.status} ${t.statusText}`);
                }
                return t.json();
            })
            .then(t => {
                const tok = t as SocialToken;
                const newToken = {
                    accessToken: tok.accessToken,
                    refreshToken: tok.refreshToken,
                    isExpired: false,
                    expiresIn: new Date(new Date().getTime() + tok.expiresIn * 1000)
                } as Token;
                
                // Use TokenStorageService (saves Token type with expiresIn: Date)
                this.tokenStorage.saveToken(newToken);
                
                // ✅ SUCCESS: Clear PKCE after successful token exchange
                this.pkceStorage.clearAll(providerName);
                console.log(`PKCE cleared for ${providerName} after successful token exchange`);
                
                fetch(`${this.settings.apiUri}/api/Authentication/Social/User`, {
                    headers: { Authorization: `Bearer ${tok.accessToken}` }
                })
                    .then(x => x.json())
                    .then(x => {
                        // Use UserStorageService instead of direct localStorage
                        this.userStorage.saveUser(x);
                        this.refresher();
                    })
                    .catch(() => {
                        // ❌ ERROR fetching user: Clear token AND user, regenerate PKCE for retry
                        this.tokenStorage.clearToken();
                        this.userStorage.clearUser();
                        this.pkceStorage.clearAll(providerName);
                        console.log(`PKCE cleared for ${providerName} after user fetch error (ready for retry)`);
                        this.settings.onLoginFailure({ code: 10, message: "error getting user.", provider: provider });
                    });
                return tok;
            })
            .catch((error) => {
                // ❌ ERROR: Token exchange failed, clear PKCE to allow retry
                this.pkceStorage.clearAll(providerName);
                console.log(`PKCE cleared for ${providerName} after token exchange error (ready for retry)`, error);
                this.settings.onLoginFailure({ code: 15, message: "error getting token.", provider: provider });
                return {} as SocialToken;
            })
    }
}
