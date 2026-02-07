import { PlatformType } from "../models/setup/PlatformType";

/**
 * Detect current platform (Web, iOS, Android)
 * Works for both React DOM and React Native
 */
export function detectPlatform(): PlatformType {
    // Check if running in React Native
    if (typeof navigator !== 'undefined' && navigator.product === 'ReactNative') {
        // Try to detect iOS or Android in React Native
        const userAgent = navigator.userAgent || '';
        if (/iPad|iPhone|iPod/.test(userAgent)) {
            return PlatformType.iOS;
        }
        if (/Android/.test(userAgent)) {
            return PlatformType.Android;
        }
        // Fallback to Android for React Native if can't detect
        return PlatformType.Android;
    }
    
    // Browser environment
    if (typeof window !== 'undefined') {
        const userAgent = window.navigator.userAgent || '';
        
        // Check for mobile browsers
        if (/iPad|iPhone|iPod/.test(userAgent)) {
            return PlatformType.iOS;
        }
        if (/Android/.test(userAgent)) {
            return PlatformType.Android;
        }
    }
    
    // Default to Web
    return PlatformType.Web;
}

/**
 * Get default redirect URI based on platform
 */
export function getDefaultRedirectUri(platform: PlatformType, webBase?: string): string {
    switch (platform) {
        case PlatformType.Web:
            if (typeof window !== 'undefined') {
                return window.location.origin;
            }
            return webBase || 'http://localhost:3000';
            
        case PlatformType.iOS:
            // Default iOS deep link format
            // Users should override this with their actual bundle ID
            return 'msauth://com.yourapp.bundle/auth';
            
        case PlatformType.Android:
            // Default Android deep link format
            // Users should override this with their actual app scheme
            return 'myapp://oauth/callback';
            
        default:
            return webBase || 'http://localhost:3000';
    }
}

/**
 * Check if running on mobile platform
 */
export function isMobilePlatform(platform: PlatformType): boolean {
    return platform === PlatformType.iOS || platform === PlatformType.Android;
}

/**
 * Check if running in React Native
 */
export function isReactNative(): boolean {
    return typeof navigator !== 'undefined' && navigator.product === 'ReactNative';
}

/**
 * Build complete redirect URI from settings with smart detection
 * 
 * Logic:
 * 1. If redirectPath contains "://" → complete URI (mobile or custom)
 * 2. If redirectPath starts with "/" → relative path, prepend window.location.origin
 * 3. If redirectPath is empty → default to window.location.origin + "/account/login"
 * 
 * @param settings Social login settings
 * @returns Complete redirect URI
 */
export function buildRedirectUri(settings: any): string {
    const redirectPath = settings.platform?.redirectPath;
    
    // If redirectPath is specified
    if (redirectPath) {
        // If contains "://", it's a complete URI (mobile deep link or custom domain)
        if (redirectPath.includes('://')) {
            return redirectPath;
        }
        
        // If starts with "/", it's a relative path for web
        if (redirectPath.startsWith('/')) {
            return `${window.location.origin}${redirectPath}`;
        }
        
        // Fallback: assume it's a relative path without leading slash
        return `${window.location.origin}/${redirectPath}`;
    }
    
    // Default: current origin + /account/login
    return `${window.location.origin}/account/login`;
}
