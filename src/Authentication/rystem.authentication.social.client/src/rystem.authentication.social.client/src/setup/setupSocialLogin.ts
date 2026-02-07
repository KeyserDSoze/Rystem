import { SocialLoginErrorResponse, SocialLoginSettings, SocialParameter, SocialLoginManager } from "..";
import { LoginMode } from "../models/setup/LoginMode";
import { PlatformType } from "../models/setup/PlatformType";
import { detectPlatform, isMobilePlatform } from "../utils/platform";
import { LocalStorageService } from "../services/LocalStorageService";
import { WindowRoutingService } from "../services/WindowRoutingService";
import { BrowserPlatformService } from "../services/BrowserPlatformService";

/**
 * Setup social login authentication
 * 
 * ⚠️ **Important**: You MUST configure the following services:
 * - `storageService` (IStorageService)
 * - `routingService` (IRoutingService)
 * - `platformService` (IPlatformService)
 * 
 * For **web applications**, use the helper method:
 * ```typescript
 * setupSocialLogin(x => {
 *     x.useBrowserDefaults(); // ✅ Sets up localStorage, window routing, and browser platform
 *     x.apiUri = "https://api.example.com";
 *     x.microsoft.clientId = "your-client-id";
 * });
 * ```
 * 
 * For **React Native**, provide custom implementations:
 * ```typescript
 * setupSocialLogin(x => {
 *     x.storageService = new ReactNativeStorageService();
 *     x.routingService = new ReactNativeRoutingService();
 *     x.platformService = new ReactNativePlatformService();
 *     // ... rest of config
 * });
 * ```
 */
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
        apiUri: getDefaultApiUri(),
        title: null,
        onLoginFailure: (data: SocialLoginErrorResponse) => { console.log(data.code); },
        automaticRefresh: false,

        // ❌ Temporarily undefined - MUST be configured before validation
        storageService: undefined as any,
        routingService: undefined as any,
        platformService: undefined as any,

        platform: {
            type: PlatformType.Auto,
            redirectPath: undefined,
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

        // ✅ Helper method for web applications
        useBrowserDefaults: function(this: SocialLoginSettings) {
            this.storageService = new LocalStorageService();
            this.routingService = new WindowRoutingService();
            this.platformService = new BrowserPlatformService();
        }
    } as SocialLoginSettings;

    settings(parameters);

    // ✅ VALIDATION: Ensure required services are configured
    const missingServices: string[] = [];

    if (!parameters.storageService) {
        missingServices.push('storageService (IStorageService)');
    }
    if (!parameters.routingService) {
        missingServices.push('routingService (IRoutingService)');
    }
    if (!parameters.platformService) {
        missingServices.push('platformService (IPlatformService)');
    }

    if (missingServices.length > 0) {
        const errorMessage = `
🚨 Missing required services in setupSocialLogin():

${missingServices.map(s => `  ❌ ${s}`).join('\n')}

📖 Solutions:

1️⃣  For WEB applications, use the helper method:
   setupSocialLogin(x => {
       x.useBrowserDefaults(); // ✅ Auto-configures browser services
       x.apiUri = "https://api.example.com";
       x.microsoft.clientId = "your-client-id";
   });

2️⃣  For REACT NATIVE, provide custom implementations:
   setupSocialLogin(x => {
       x.storageService = new ReactNativeStorageService();
       x.routingService = new ReactNativeRoutingService();
       x.platformService = new ReactNativePlatformService();
       // ... rest of config
   });

📚 Documentation: https://github.com/KeyserDSoze/Rystem/blob/master/src/Authentication/rystem.authentication.social.client/README.md
        `.trim();

        throw new Error(errorMessage);
    }

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
