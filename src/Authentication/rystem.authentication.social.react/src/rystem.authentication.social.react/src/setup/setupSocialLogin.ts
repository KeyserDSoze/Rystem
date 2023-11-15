import { SocialLoginErrorResponse, SocialLoginSettings, SocialParameter, SocialLoginManager } from "..";

export const setupSocialLogin = function (settings: (settings: SocialLoginSettings) => void): SocialLoginManager {
    const url = new URL(window.location.href);
    const baseUri = `${url.protocol}//${url.host}`;
    const parameters = {
        apiUri: baseUri,
        title: null,
        onLoginFailure: (data: SocialLoginErrorResponse) => { console.log(data.code); },
        redirectDomain: baseUri,
        google: {} as SocialParameter,
        microsoft: {} as SocialParameter,
        facebook: {} as SocialParameter,
        github: {} as SocialParameter,
        amazon: {} as SocialParameter,
        linkedin: {} as SocialParameter,
        x: {} as SocialParameter,
        instagram: {} as SocialParameter,
        pinterest: {} as SocialParameter,
        tiktok: {} as SocialParameter,
    } as SocialLoginSettings;
    settings(parameters);
    return SocialLoginManager.Instance(parameters);
};
