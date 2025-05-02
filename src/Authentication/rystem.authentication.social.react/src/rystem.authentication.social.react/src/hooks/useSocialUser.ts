import { useSocialToken, SocialUser, getSocialLoginSettings } from "..";
export function useSocialUser<T = {}>(): SocialUser<T> {
    const settings = getSocialLoginSettings();
    const token = useSocialToken();
    if (!token.isExpired) {
        const userAsJson = localStorage.getItem("socialUser");
        if (userAsJson != null) {
            let currentJson = JSON.parse(userAsJson);
            if (settings.identityTransformer?.fromPlain != null)
                currentJson = settings.identityTransformer.fromPlain(currentJson);
            const user = currentJson as SocialUser<T>;
            if (settings.identityTransformer?.retrieveUsername == null)
                return { ...user, isAuthenticated: true };
            else
                return { ...user, isAuthenticated: true, username: settings.identityTransformer.retrieveUsername(user) };
        }
    }

    return { isAuthenticated: false } as SocialUser<T>;
}

