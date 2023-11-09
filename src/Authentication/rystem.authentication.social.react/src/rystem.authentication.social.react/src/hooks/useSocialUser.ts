import { useSocialToken } from "./useSocialToken";
import { SocialUser } from "../models/SocialUser";
export const useSocialUser = function (): SocialUser {
    const token = useSocialToken();
    if (!token.isExpired) {
        const userAsJson = localStorage.getItem("socialUser");
        if (userAsJson != null) {
            const user = JSON.parse(userAsJson) as SocialUser;
            user.isAuthenticated = true;
            return user;
        }
    }
    return { isAuthenticated: false } as SocialUser;
};
