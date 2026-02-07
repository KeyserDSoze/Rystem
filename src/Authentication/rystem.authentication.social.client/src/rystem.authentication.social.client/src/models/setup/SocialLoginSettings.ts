import { SocialLoginErrorResponse, SocialParameter } from "../..";
import { PlatformConfig } from "./PlatformConfig";
import { IStorageService } from "../../services/IStorageService";
import { IRoutingService } from "../../services/IRoutingService";
import { IPlatformService } from "../../services/IPlatformService";

export interface SocialLoginSettings {
    apiUri: string;

    automaticRefresh: boolean;
    identityTransformer?: IIdentityTransformer<any>;
    onLoginFailure: (data: SocialLoginErrorResponse) => void;
    title: string | null;

    /**
     * Storage service for persisting tokens, PKCE verifiers, etc.
     * Default: LocalStorageService (browser localStorage)
     * 
     * @example Custom secure storage for mobile
     * storageService: new SecureStorageService()
     */
    storageService: IStorageService;

    /**
     * Routing service for URL parameter reading and navigation
     * Default: WindowRoutingService (uses window.location and window.history)
     * 
     * IMPORTANT: Required for React Router, Next.js App Router, and other client-side routing frameworks
     * to properly handle OAuth callbacks, redirects, and return URLs.
     * 
     * @example React Router with useSearchParams, useNavigate, and useLocation hooks
     * routingService: new ReactRouterRoutingService()
     * 
     * @example Next.js App Router with useRouter, usePathname, and useSearchParams
     * routingService: new NextAppRouterRoutingService()
     */
    routingService: IRoutingService;

    /**
     * Platform service for environment-specific operations (events, dimensions, scripts)
     * Default: BrowserPlatformService (uses window, document, DOM APIs)
     * 
     * Used by UI components for:
     * - Storage event listeners (popup ↔ main window communication)
     * - Screen dimensions (popup positioning)
     * - External script loading (Google SDK, Facebook SDK)
     * 
     * @example React Native custom implementation
     * platformService: new ReactNativePlatformService()
     * 
     * @default BrowserPlatformService
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
}

export interface IIdentityTransformer<TIdentity> {
    toPlain: (input: TIdentity | any) => any;
    fromPlain: (input: any) => TIdentity;
    retrieveUsername: (input: TIdentity) => string;
}