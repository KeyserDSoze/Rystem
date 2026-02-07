/**
 * Unified Routing Service for React Router v6+
 * 
 * This service handles both URL parameter reading and navigation for React Router.
 * Copy this file to your project and remove the `.example` extension.
 * 
 * IMPORTANT: Must be initialized inside a React component with routing context.
 * 
 * @example
 * ```typescript
 * import { ReactRouterRoutingService } from './ReactRouterRoutingService';
 * import { useSearchParams, useNavigate, useLocation } from 'react-router-dom';
 * 
 * const routingService = new ReactRouterRoutingService();
 * 
 * setupSocialLogin(x => {
 *     x.routingService = routingService;
 * });
 * 
 * function App() {
 *     const [searchParams] = useSearchParams();
 *     const navigate = useNavigate();
 *     const location = useLocation();
 *     
 *     useEffect(() => {
 *         // Single initialization with all hooks
 *         routingService.initialize(() => searchParams, navigate, location);
 *     }, [searchParams, navigate, location]);
 *     
 *     return <div>Your app</div>;
 * }
 * ```
 */

import type { IRoutingService } from './IRoutingService';

/**
 * Routing Service for React Router v6+
 * Uses useSearchParams, useNavigate, and useLocation hooks
 */
export class ReactRouterRoutingService implements IRoutingService {
    private searchParamsGetter: (() => URLSearchParams) | null = null;
    private navigateFunc: ((to: string, options?: any) => void) | null = null;
    private location: any = null;

    /**
     * Initialize with React Router hooks
     * MUST be called inside a React component with routing context
     * 
     * @param searchParamsGetter - Function that returns URLSearchParams from useSearchParams()
     * @param navigateFunc - The navigate function from useNavigate()
     * @param location - The location object from useLocation()
     * 
     * @example
     * ```typescript
     * const [searchParams] = useSearchParams();
     * const navigate = useNavigate();
     * const location = useLocation();
     * 
     * useEffect(() => {
     *     routingService.initialize(() => searchParams, navigate, location);
     * }, [searchParams, navigate, location]);
     * ```
     */
    initialize(
        searchParamsGetter: () => URLSearchParams,
        navigateFunc: (to: string, options?: any) => void,
        location: any
    ): void {
        this.searchParamsGetter = searchParamsGetter;
        this.navigateFunc = navigateFunc;
        this.location = location;
    }

    // ===== URL Parameter Reading =====

    getSearchParam(key: string): string | null {
        if (!this.searchParamsGetter) {
            console.warn('ReactRouterRoutingService not initialized. Call initialize() with React Router hooks.');
            return null;
        }
        return this.searchParamsGetter().get(key);
    }

    getAllSearchParams(): URLSearchParams {
        if (!this.searchParamsGetter) {
            console.warn('ReactRouterRoutingService not initialized. Call initialize() with React Router hooks.');
            return new URLSearchParams();
        }
        return this.searchParamsGetter();
    }

    // ===== Navigation Operations =====

    getCurrentPath(): string {
        if (!this.location) {
            console.warn('ReactRouterRoutingService not initialized. Falling back to window.location.');
            return window.location.pathname + window.location.search;
        }
        return this.location.pathname + this.location.search;
    }

    navigateTo(url: string): void {
        // External OAuth redirects (https://...) must use window.location
        if (url.startsWith('http://') || url.startsWith('https://')) {
            console.log('🌐 [ReactRouter] External OAuth redirect, using window.location:', url);
            window.location.href = url;
        } else if (this.navigateFunc) {
            console.log('🔄 [ReactRouter] Internal navigation:', url);
            this.navigateFunc(url);
        } else {
            console.warn('ReactRouterRoutingService not initialized. Using window.location.');
            window.location.href = url;
        }
    }

    navigateReplace(path: string): void {
        if (this.navigateFunc) {
            console.log('🔄 [ReactRouter] Replacing URL:', path);
            this.navigateFunc(path, { replace: true });
        } else {
            console.warn('ReactRouterRoutingService not initialized. Using window.history.');
            window.history.replaceState({}, '', path);
        }
    }

    openPopup(url: string, name: string, features: string): Window | null {
        return window.open(url, name, features);
    }
}
