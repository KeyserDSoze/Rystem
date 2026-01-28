import { SocialLoginErrorResponse, SocialParameter } from "../..";
import { PlatformConfig } from "./PlatformConfig";
import { IStorageService } from "../../services/IStorageService";

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