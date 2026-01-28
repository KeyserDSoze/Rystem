/**
 * Navigation Service implementation for React Router v6+
 * 
 * This file is an example implementation for React Router.
 * Copy this to your project and use it with setupSocialLogin.
 * 
 * IMPORTANT: This service MUST be initialized inside a React component
 * that has access to useNavigate and useLocation hooks.
 * 
 * @example
 * ```typescript
 * import { ReactRouterNavigationService } from './ReactRouterNavigationService';
 * import { useNavigate, useLocation } from 'react-router-dom';
 * 
 * const reactRouterNavService = new ReactRouterNavigationService();
 * 
 * setupSocialLogin(x => {
 *     x.navigationService = reactRouterNavService;
 * });
 * 
 * function App() {
 *     const navigate = useNavigate();
 *     const location = useLocation();
 *     useEffect(() => {
 *         reactRouterNavService.initialize(navigate, location);
 *     }, [navigate, location]);
 *     return <div>Your app</div>;
 * }
 * ```
 */

import type { INavigationService } from './INavigationService';

/**
 * Navigation Service for React Router v6+
 * Uses useNavigate and useLocation hooks for client-side routing
 */
export class ReactRouterNavigationService implements INavigationService {
    private navigateFunc: ((to: string, options?: any) => void) | null = null;
    private location: any = null;

    /**
     * Initialize with React Router hooks
     * MUST be called inside a React component that has routing context
     * 
     * @param navigateFunc - The navigate function from useNavigate() hook
     * @param location - The location object from useLocation() hook
     * 
     * @example
     * ```typescript
     * const navigate = useNavigate();
     * const location = useLocation();
     * useEffect(() => {
     *     reactRouterNavService.initialize(navigate, location);
     * }, [navigate, location]);
     * ```
     */
    initialize(navigateFunc: (to: string, options?: any) => void, location: any): void {
        this.navigateFunc = navigateFunc;
        this.location = location;
    }

    /**
     * Get current path from React Router location
     * @returns Current route path with query parameters
     */
    getCurrentPath(): string {
        if (!this.location) {
            console.warn('ReactRouterNavigationService not initialized. Falling back to window.location.');
            return window.location.pathname + window.location.search;
        }
        return this.location.pathname + this.location.search;
    }

    /**
     * Navigate to URL using React Router or window.location for external URLs
     * @param url - Full URL (external OAuth) or path (internal navigation)
     */
    navigateTo(url: string): void {
        // For external OAuth redirects, we must use window.location
        if (url.startsWith('http://') || url.startsWith('https://')) {
            console.log('🌐 [ReactRouter] External URL detected, using window.location:', url);
            window.location.href = url;
        } else if (this.navigateFunc) {
            console.log('🔄 [ReactRouter] Internal navigation:', url);
            this.navigateFunc(url);
        } else {
            console.warn('ReactRouterNavigationService not initialized. Using window.location.');
            window.location.href = url;
        }
    }

    /**
     * Replace current URL using React Router navigate
     * @param path - Path to replace current URL with
     */
    navigateReplace(path: string): void {
        if (this.navigateFunc) {
            console.log('🔄 [ReactRouter] Replacing URL:', path);
            this.navigateFunc(path, { replace: true });
        } else {
            console.warn('ReactRouterNavigationService not initialized. Using window.history.');
            window.history.replaceState({}, '', path);
        }
    }

    /**
     * Open URL in popup window (OAuth popup mode)
     * @param url - URL to open
     * @param name - Window name
     * @param features - Window features
     * @returns Window reference or null
     */
    openPopup(url: string, name: string, features: string): Window | null {
        return window.open(url, name, features);
    }
}
