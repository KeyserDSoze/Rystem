import { ProviderType, SocialLoginSettings, SocialToken, Token } from "..";
import { getAndRemoveCodeVerifier } from "../utils/pkce";

export class SocialLoginManager {
    private static instance: SocialLoginManager | null;
    public settings: SocialLoginSettings;
    public refresher: () => void;
    
    private constructor(settings: SocialLoginSettings | null) {
        this.settings = settings ?? {} as SocialLoginSettings;
        this.refresher = () => { };
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
        // Check if we have a code_verifier stored (for PKCE)
        const codeVerifier = getAndRemoveCodeVerifier(ProviderType[provider].toLowerCase());
        
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
                localStorage.setItem("socialUserToken", JSON.stringify(newToken));
                
                fetch(`${this.settings.apiUri}/api/Authentication/Social/User`, {
                    headers: { Authorization: `Bearer ${tok.accessToken}` }
                })
                    .then(x => x.json())
                    .then(x => {
                        localStorage.setItem("socialUser", JSON.stringify(x));
                        this.refresher();
                    })
                    .catch(() => {
                        localStorage.removeItem("socialUserToken");
                        this.settings.onLoginFailure({ code: 10, message: "error getting user.", provider: provider });
                    });
                return tok;
            })
            .catch(() => {
                this.settings.onLoginFailure({ code: 15, message: "error getting token.", provider: provider });
                return {} as SocialToken;
            })
    }
}
