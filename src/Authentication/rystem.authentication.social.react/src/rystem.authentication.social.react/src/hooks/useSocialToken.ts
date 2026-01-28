import { ProviderType, SocialLoginManager, Token, getSocialLoginSettings } from "..";
import { TokenStorageService } from "../services/TokenStorageService";

export const useSocialToken = function (): Token {
    const settings = getSocialLoginSettings();
    const tokenStorage = new TokenStorageService(settings.storageService);
    
    const currentToken = tokenStorage.getToken();
    
    if (currentToken) {
        // Check if expired
        currentToken.isExpired = currentToken.expiresIn.getTime() < new Date().getTime();
        
        // Automatic refresh if expired and enabled
        if (currentToken.isExpired && settings.automaticRefresh) {
            SocialLoginManager.Instance(null).updateToken(ProviderType.DotNet, currentToken.refreshToken);
        }
        
        return currentToken;
    }
    
    return { isExpired: true } as Token;
};

