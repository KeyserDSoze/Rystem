import { SocialLoginSettings } from "../models/setup/SocialLoginSettings";
import { SocialLoginManager } from "./SocialLoginManager";

export const getSocialLoginSettings = function(): SocialLoginSettings {
    return SocialLoginManager.Instance(null).settings;
};
