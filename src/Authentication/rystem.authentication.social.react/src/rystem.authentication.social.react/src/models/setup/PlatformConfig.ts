import { LoginMode } from "./LoginMode";
import { PlatformType } from "./PlatformType";

/**
 * Platform-specific configuration for social authentication
 */
export interface PlatformConfig {
    /**
     * Platform type (web, ios, android, auto)
     */
    type: PlatformType;
    
    /**
     * Redirect path or complete URI for OAuth callback
     * 
     * **Smart detection:**
     * - If contains "://" → treated as complete URI (e.g., "myapp://oauth/callback" for mobile)
     * - If starts with "/" → treated as path, prepended with window.location.origin (e.g., "/account/login" for web)
     * - Default: "/account/login"
     * 
     * **Examples:**
     * - Web: "/account/login" → becomes "https://yourdomain.com/account/login"
     * - iOS: "msauth://com.yourapp.bundle/auth" → used as-is
     * - Android: "myapp://oauth/callback" → used as-is
     * - Custom web: "https://custom.domain.com/callback" → used as-is
     */
    redirectPath?: string;
    
    /**
     * Login mode (popup or redirect)
     * Default: Popup for web, Redirect for mobile
     */
    loginMode?: LoginMode;
}

/**
 * Platform selector function for React Native compatibility
 * Similar to React Native Platform.select()
 */
export type PlatformSelector<T> = {
    web?: T;
    ios?: T;
    android?: T;
    default?: T;
};

/**
 * Helper to select value based on platform
 */
export function selectByPlatform<T>(
    currentPlatform: PlatformType,
    selector: PlatformSelector<T>
): T | undefined {
    if (currentPlatform === PlatformType.Web && selector.web !== undefined) {
        return selector.web;
    }
    if (currentPlatform === PlatformType.iOS && selector.ios !== undefined) {
        return selector.ios;
    }
    if (currentPlatform === PlatformType.Android && selector.android !== undefined) {
        return selector.android;
    }
    return selector.default;
}
