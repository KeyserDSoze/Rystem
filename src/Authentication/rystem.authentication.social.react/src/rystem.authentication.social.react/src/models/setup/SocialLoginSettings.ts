import { SocialLoginErrorResponse, SocialParameter } from "../..";
import { PlatformConfig } from "./PlatformConfig";
import { IStorageService } from "../../services/IStorageService";
import { IUrlService } from "../../services/IUrlService";
import { INavigationService } from "../../services/INavigationService";

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
     * URL service for reading URL parameters (supports React Router, Next.js, etc.)
     * Default: WindowUrlService (uses window.location.search)
     * 
     * @example React Router with useSearchParams hook
     * urlService: new ReactRouterUrlService()
     * 
     * @example Next.js App Router with useSearchParams
     * urlService: new NextJsUrlService()
     */
    urlService: IUrlService;

    /**
     * Navigation service for routing and URL manipulation (supports React Router, Next.js, etc.)
     * Default: WindowNavigationService (uses window.location and window.history)
     * 
     * IMPORTANT: Required for React Router, Next.js App Router, and other client-side routing frameworks
     * to properly handle OAuth redirects and return URLs.
     * 
     * @example React Router with useNavigate hook
     * navigationService: new ReactRouterNavigationService()
     * 
     * @example Next.js App Router with useRouter
     * navigationService: new NextJsNavigationService()
     */
    navigationService: INavigationService;

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