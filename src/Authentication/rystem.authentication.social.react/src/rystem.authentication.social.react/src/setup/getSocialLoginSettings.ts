import { SocialLoginSettings, SocialLoginManager } from "..";

export const getSocialLoginSettings = function(): SocialLoginSettings {
    return SocialLoginManager.Instance(null).settings;
};
