/**
 * URL Service implementation for React Router v6+
 * 
 * This file is an example implementation for React Router.
 * Copy this to your project and use it with setupSocialLogin.
 * 
 * @example
 * ```typescript
 * import { ReactRouterUrlService } from './ReactRouterUrlService';
 * import { useSearchParams } from 'react-router-dom';
 * 
 * const reactRouterUrlService = new ReactRouterUrlService();
 * 
 * setupSocialLogin(x => {
 *     x.urlService = reactRouterUrlService;
 * });
 * 
 * function App() {
 *     const [searchParams] = useSearchParams();
 *     useEffect(() => {
 *         reactRouterUrlService.initialize(() => searchParams);
 *     }, [searchParams]);
 *     return <div>Your app</div>;
 * }
 * ```
 */

import type { IUrlService } from './IUrlService';

/**
 * URL Service for React Router v6+
 * Uses useSearchParams hook to read URL parameters from client-side routing
 */
export class ReactRouterUrlService implements IUrlService {
    private searchParamsGetter: (() => URLSearchParams) | null = null;

    /**
     * Initialize with useSearchParams hook from React Router
     * MUST be called inside a React component that has access to routing context
     * 
     * @param searchParamsGetter - Function that returns current URLSearchParams from useSearchParams hook
     * 
     * @example
     * ```typescript
     * const [searchParams] = useSearchParams();
     * useEffect(() => {
     *     reactRouterUrlService.initialize(() => searchParams);
     * }, [searchParams]);
     * ```
     */
    initialize(searchParamsGetter: () => URLSearchParams): void {
        this.searchParamsGetter = searchParamsGetter;
    }

    /**
     * Get a single URL parameter value
     * @param key - Parameter name (e.g., 'code', 'state')
     * @returns Parameter value or null if not found
     */
    getSearchParam(key: string): string | null {
        if (!this.searchParamsGetter) {
            console.warn(
                'ReactRouterUrlService not initialized. ' +
                'Call initialize() with useSearchParams() hook inside a React component.'
            );
            return null;
        }
        
        const params = this.searchParamsGetter();
        return params.get(key);
    }

    /**
     * Get all URL parameters
     * @returns URLSearchParams object with all parameters
     */
    getAllSearchParams(): URLSearchParams {
        if (!this.searchParamsGetter) {
            console.warn(
                'ReactRouterUrlService not initialized. ' +
                'Call initialize() with useSearchParams() hook inside a React component.'
            );
            return new URLSearchParams();
        }
        
        return this.searchParamsGetter();
    }
}
