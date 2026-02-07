import { getSocialLoginSettings } from "..";
import { TokenStorageService } from "../services/TokenStorageService";
import { UserStorageService } from "../services/UserStorageService";

export const removeSocialLogin = function(): void {
    const settings = getSocialLoginSettings();
    const tokenStorage = new TokenStorageService(settings.storageService);
    const userStorage = new UserStorageService(settings.storageService);
    
    tokenStorage.clearToken();
    userStorage.clearUser();
};
