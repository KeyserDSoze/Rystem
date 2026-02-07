import { SocialLoginErrorResponse, SocialLoginSettings, SocialParameter, SocialLoginManager } from "..";
import { LoginMode } from "../models/setup/LoginMode";
import { PlatformType } from "../models/setup/PlatformType";
import { detectPlatform, isMobilePlatform } from "../utils/platform";
import { LocalStorageService } from "../services/LocalStorageService";
import { WindowRoutingService } from "../services/WindowRoutingService";
import { BrowserPlatformService } from "../services/BrowserPlatformService";

export const setupSocialLogin = function (settings: (settings: SocialLoginSettings) => void): SocialLoginManager {
// ✅ Lazy evaluation: only access window when actually needed
const getDefaultApiUri = (): string => {
    if (typeof window !== 'undefined' && window.location) {
        return window.location.origin;
    }
    // Fallback for React Native or SSR
    return '';
};

const parameters = {
    apiUri: getDefaultApiUri(),  // ✅ Safe: checks window existence
    title: null,
    onLoginFailure: (data: SocialLoginErrorResponse) => { console.log(data.code); },
    automaticRefresh: false,
    storageService: new LocalStorageService(),   // Default: localStorage
    routingService: new WindowRoutingService(),  // Default: window.location + window.history
    platformService: new BrowserPlatformService(), // Default: browser DOM/BOM APIs
    platform: {
        type: PlatformType.Auto,
        redirectPath: undefined,  // Smart default: will use window.location.origin + "/account/login"
        loginMode: LoginMode.Popup
    },
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
    
    // Auto-detect platform if set to 'auto'
    if (!parameters.platform) {
        parameters.platform = {
            type: PlatformType.Auto,
            loginMode: LoginMode.Popup
        };
    }
    
    if (parameters.platform.type === PlatformType.Auto) {
        parameters.platform.type = detectPlatform();
    }
    
    // Set default loginMode based on platform if not specified
    if (!parameters.platform.loginMode) {
        // Default: popup for web, redirect for mobile
        parameters.platform.loginMode = isMobilePlatform(parameters.platform.type) 
            ? LoginMode.Redirect 
            : LoginMode.Popup;
    }
    
    return SocialLoginManager.Instance(parameters);
};
