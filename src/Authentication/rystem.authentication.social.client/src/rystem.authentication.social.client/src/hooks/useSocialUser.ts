import { useSocialToken, SocialUser, getSocialLoginSettings } from "..";
import { UserStorageService } from "../services/UserStorageService";

export function useSocialUser<T = {}>(): SocialUser<T> {
    const settings = getSocialLoginSettings();
    const token = useSocialToken();
    const userStorage = new UserStorageService(settings.storageService);
    
    if (!token.isExpired) {
        const user = userStorage.getUser<T>();
        
        if (user != null) {
            let currentUser = user;
            
            // Apply identity transformation if configured
            if (settings.identityTransformer?.fromPlain != null) {
                currentUser = settings.identityTransformer.fromPlain(currentUser);
            }
            
            // Extract username if transformer provided
            if (settings.identityTransformer?.retrieveUsername != null) {
                return { 
                    ...currentUser, 
                    isAuthenticated: true, 
                    username: settings.identityTransformer.retrieveUsername(currentUser) 
                };
            }
            
            return { ...currentUser, isAuthenticated: true };
        }
    }

    return { isAuthenticated: false } as SocialUser<T>;
}

