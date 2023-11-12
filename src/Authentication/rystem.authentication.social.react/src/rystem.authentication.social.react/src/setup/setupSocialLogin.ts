import { SocialLoginErrorResponse, SocialLoginSettings, SocialParameter, SocialLoginManager } from "..";

export const setupSocialLogin = function (settings: (settings: SocialLoginSettings) => void): SocialLoginManager {
    const url = new URL(window.location.href);
    const baseUri = `${url.protocol}//${url.host}`;
    const parameters = {
        apiUri: baseUri,
        title: null,
        onLoginFailure: (data: SocialLoginErrorResponse) => { console.log(data.code); },
        redirectDomain: baseUri,
        google: { indexOrder: 0 } as SocialParameter,
        microsoft: { indexOrder: 1 } as SocialParameter,
        facebook: { indexOrder: 2 } as SocialParameter,
        github: { indexOrder: 3 } as SocialParameter,
        amazon: { indexOrder: 4 } as SocialParameter,
        linkedin: { indexOrder: 5 } as SocialParameter,
        x: { indexOrder: 6 } as SocialParameter,
        instagram: { indexOrder: 7 } as SocialParameter,
        pinterest: { indexOrder: 8 } as SocialParameter,
    } as SocialLoginSettings;
    settings(parameters);
    return SocialLoginManager.Instance(parameters);
};
