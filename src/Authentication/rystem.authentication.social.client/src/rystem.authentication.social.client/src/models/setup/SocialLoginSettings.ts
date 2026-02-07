import { SocialLoginErrorResponse, SocialParameter } from "../..";
import { PlatformConfig } from "./PlatformConfig";
import { IStorageService } from "../../services/IStorageService";
import { IRoutingService } from "../../services/IRoutingService";
import { IPlatformService } from "../../services/IPlatformService";
import { LocalStorageService } from "../../services/LocalStorageService";
import { WindowRoutingService } from "../../services/WindowRoutingService";
import { BrowserPlatformService } from "../../services/BrowserPlatformService";

export interface SocialLoginSettings {
    apiUri: string;

    automaticRefresh: boolean;
    identityTransformer?: IIdentityTransformer<any>;
    onLoginFailure: (data: SocialLoginErrorResponse) => void;
    title: string | null;

    /**
     * Storage service for persisting tokens, PKCE verifiers, etc.
     * 
     * ⚠️ **Required**: Must be configured explicitly or via `useBrowserDefaults()`.
     * 
     * Web: Use `x.useBrowserDefaults()` or manually set `new LocalStorageService()`
     * React Native: Implement `IStorageService` with AsyncStorage
     * 
     * @example Web (automatic)
     * x.useBrowserDefaults();
     * 
     * @example Web (manual)
     * x.storageService = new LocalStorageService();
     * 
     * @example React Native
     * x.storageService = new ReactNativeStorageService();
     */
    storageService: IStorageService;

    /**
     * Routing service for URL parameter reading and navigation
     * 
     * ⚠️ **Required**: Must be configured explicitly or via `useBrowserDefaults()`.
     * 
     * Web: Use `x.useBrowserDefaults()` or manually set `new WindowRoutingService()`
     * React Native: Implement `IRoutingService` with Linking API
     * 
     * IMPORTANT: Required for React Router, Next.js App Router, and other client-side routing frameworks
     * to properly handle OAuth callbacks, redirects, and return URLs.
     * 
     * @example Web (automatic)
     * x.useBrowserDefaults();
     * 
     * @example Web (manual)
     * x.routingService = new WindowRoutingService();
     * 
     * @example React Router
     * x.routingService = new ReactRouterRoutingService();
     * 
     * @example Next.js App Router
     * x.routingService = new NextAppRouterRoutingService();
     * 
     * @example React Native
     * x.routingService = new ReactNativeRoutingService();
     */
    routingService: IRoutingService;

    /**
     * Platform service for environment-specific operations (events, dimensions, scripts)
     * 
     * ⚠️ **Required**: Must be configured explicitly or via `useBrowserDefaults()`.
     * 
     * Web: Use `x.useBrowserDefaults()` or manually set `new BrowserPlatformService()`
     * React Native: Implement `IPlatformService` with Dimensions, EventEmitter
     * 
     * Used by UI components for:
     * - Storage event listeners (popup ↔ main window communication)
     * - Screen dimensions (popup positioning)
     * - External script loading (Google SDK, Facebook SDK)
     * - Window operations (popup detection, closing)
     * 
     * @example Web (automatic)
     * x.useBrowserDefaults();
     * 
     * @example Web (manual)
     * x.platformService = new BrowserPlatformService();
     * 
     * @example React Native
     * x.platformService = new ReactNativePlatformService();
     */
    platformService: IPlatformService;

    /**
     * Platform configuration (Web, iOS, Android)
     * 
     * @example Web configuration (auto-detect domain)
     * platform: {
     *   type: PlatformType.Web,
     *   redirectPath: "/account/login"
     * }
     * 
     * @example React Native iOS configuration
     * platform: {
     *   type: PlatformType.iOS,
     *   redirectPath: "myapp://oauth/callback"  // Complete deep link
     * }
     * 
     * @default { type: 'auto', redirectPath: '/account/login', loginMode: 'popup' }
     */
    platform?: PlatformConfig;

    google: SocialParameter;
    microsoft: SocialParameter;
    facebook: SocialParameter;
    github: SocialParameter;
    amazon: SocialParameter;
    linkedin: SocialParameter;
    x: SocialParameter;
    instagram: SocialParameter;
    pinterest: SocialParameter;
    tiktok: SocialParameter;

    /**
     * 🚀 Helper method for WEB applications
     * 
     * Automatically configures browser-based services:
     * - `storageService`: LocalStorageService (uses browser localStorage)
     * - `routingService`: WindowRoutingService (uses window.location and window.history)
     * - `platformService`: BrowserPlatformService (uses window, document, DOM APIs)
     * 
     * ⚠️ **Web only**: Do NOT call this in React Native applications!
     * 
     * @example Basic web setup
     * setupSocialLogin(x => {
     *     x.useBrowserDefaults(); // ✅ One line to configure all browser services
     *     x.apiUri = "https://api.example.com";
     *     x.microsoft.clientId = "your-client-id";
     * });
     * 
     * @example With custom routing (React Router, Next.js)
     * setupSocialLogin(x => {
     *     x.useBrowserDefaults();
     *     x.routingService = new ReactRouterRoutingService(); // Override routing only
     *     // ... rest of config
     * });
     */
    useBrowserDefaults(): void;
}

export interface IIdentityTransformer<TIdentity> {
    toPlain: (input: TIdentity | any) => any;
    fromPlain: (input: any) => TIdentity;
    retrieveUsername: (input: TIdentity) => string;
}