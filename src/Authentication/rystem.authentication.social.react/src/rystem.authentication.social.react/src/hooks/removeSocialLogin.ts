export const removeSocialLogin = function(): void {
    localStorage.removeItem("socialUserToken");
    localStorage.removeItem("socialUser");
};
