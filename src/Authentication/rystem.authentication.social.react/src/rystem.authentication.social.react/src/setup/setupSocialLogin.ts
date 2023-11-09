import { SocialLoginSettings } from "../models/setup/SocialLoginSettings";
import { SocialParameter } from "../models/setup/SocialParameter";
import { SocialLoginManager } from "./SocialLoginManager";


export const setupSocialLogin = function (settings: (settings: SocialLoginSettings) => void): SocialLoginManager {
    const url = new URL(window.location.href);
    const baseUri = `${url.protocol}//${url.host}`;
    const parameters = {
        apiUri: baseUri,
        title: null,
        redirectDomain: baseUri,
        google: { indexOrder: 0 } as SocialParameter,
        microsoft: { indexOrder: 1 } as SocialParameter,
        facebook: { indexOrder: 2 } as SocialParameter
    } as SocialLoginSettings;
    settings(parameters);
    return SocialLoginManager.Instance(parameters);
};
