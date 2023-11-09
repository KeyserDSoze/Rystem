import { SocialLoginSettings } from "../models/setup/SocialLoginSettings";
import { SocialToken } from "../models/SocialToken";
import { Token } from "../models/Token";


export class SocialLoginManager {
    private static instance: SocialLoginManager | null;
    public settings: SocialLoginSettings;
    public refresher: () => void;
    private constructor(settings: SocialLoginSettings | null) {
        this.settings = settings ?? {} as SocialLoginSettings;
        this.refresher = () => { };
    }
    public static Instance(settings: SocialLoginSettings | null): SocialLoginManager {
        if (SocialLoginManager.instance == null)
            SocialLoginManager.instance = new SocialLoginManager(settings);
        return SocialLoginManager.instance;
    }
    public updateToken(provider: number, code: string): Promise<SocialToken> {
        return fetch(`${this.settings.apiUri}/api/Authentication/Social/Token?provider=${provider}&code=${code}`)
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
                    });
                return tok;
            })
            .catch(() => {
                return {} as SocialToken;
            })
    }
}
