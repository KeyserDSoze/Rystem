export { AmazonButton } from "./buttons/singles/AmazonButton";
export { GoogleButton } from "./buttons/singles/GoogleButton";
export { MicrosoftButton } from "./buttons/singles/MicrosoftButton";
export { FacebookButton } from "./buttons/singles/FacebookButton";
export { GitHubButton } from "./buttons/singles/GitHubButton";
export { LinkedinButton } from "./buttons/singles/LinkedinButton";
export { XButton } from "./buttons/singles/XButton";
export { TikTokButton } from "./buttons/singles/TikTokButton";
export { InstagramButton } from "./buttons/singles/InstagramButton";
export { PinterestButton } from "./buttons/singles/PinterestButton";
export { SocialLogoutButton } from './buttons/SocialLogoutButton'
export { SocialLoginWrapper } from './context/SocialLoginWrapper'
export { SocialLoginContextUpdate, SocialLoginContextRefresh, SocialLoginContextLogout } from './context/SocialLoginContext'
export { useSocialToken } from './hooks/useSocialToken'
export { useSocialUser } from './hooks/useSocialUser'
export { removeSocialLogin } from './hooks/removeSocialLogin'
export type { SocialToken } from './models/SocialToken'
export type { SocialUser, ISocialUser } from './models/SocialUser'
export type { Token } from './models/Token'
export type { SocialLoginSettings, IIdentityTransformer } from './models/setup/SocialLoginSettings'
export type { SocialParameter } from './models/setup/SocialParameter'
export { getSocialLoginSettings } from './setup/getSocialLoginSettings'
export { setupSocialLogin } from './setup/setupSocialLogin'
export { SocialLoginManager } from './setup/SocialLoginManager'
export { ProviderType } from './models/setup/ProviderType'
export type { SocialLoginErrorResponse } from './models/setup/SocialLoginErrorResponse'
export { CreateSocialButton } from "./buttons/CreateSocialButton";
export { SocialLoginButtons } from './buttons/SocialLoginButtons'
export type { SocialButtonProps } from './models/SocialButtonProps'
export type { SocialButtonsProps } from "./models/SocialButtonsProps";
export { generateCodeVerifier, generateCodeChallenge } from './utils/pkce'

// Modern UI Components (styled social buttons with dark mode support)
export { ModernSocialButton } from './components/ModernSocialButton';
export type { ModernSocialButtonProps } from './components/ModernSocialButton';
export { MicrosoftIcon, GoogleIcon, FacebookIcon, GitHubIcon, XIcon, LinkedInIcon, AmazonIcon, InstagramIcon, TikTokIcon, PinterestIcon } from './components/BrandIcons';
import './styles/SocialButton.css'; // Import CSS for styled buttons

// Storage services (infrastructure layer)
export type { IStorageService } from './services/IStorageService';
export { LocalStorageService } from './services/LocalStorageService';
export { PkceStorageService } from './services/PkceStorageService';
export { TokenStorageService } from './services/TokenStorageService';
export { UserStorageService } from './services/UserStorageService';

// Routing services (infrastructure layer - unified URL reading and navigation)
export type { IRoutingService } from './services/IRoutingService';
export { WindowRoutingService } from './services/WindowRoutingService';

// Platform and login mode support
export { PlatformType } from './models/setup/PlatformType';
export { LoginMode } from './models/setup/LoginMode';
export type { PlatformConfig, PlatformSelector } from './models/setup/PlatformConfig';
export { detectPlatform, getDefaultRedirectUri, isMobilePlatform, isReactNative, buildRedirectUri } from './utils/platform';
export { selectByPlatform } from './models/setup/PlatformConfig';
