import { SocialLoginManager } from "../setup/SocialLoginManager";
import { Token } from "../models/Token";
import { getSocialLoginSettings } from "../setup/getSocialLoginSettings";


export const useSocialToken = function (): Token {
    const settings = getSocialLoginSettings();
    const token = localStorage.getItem("socialUserToken");
    if (token != null) {
        const currentToken = JSON.parse(token) as Token;
        currentToken.expiresIn = new Date(currentToken.expiresIn);
        currentToken.isExpired = currentToken.expiresIn.getTime() < new Date().getTime();
        if (currentToken.isExpired && settings.automaticRefresh) {
            SocialLoginManager.Instance(null).updateToken(0, currentToken.refreshToken);
        }
        return currentToken;
    }
    return { isExpired: true } as Token;
};

