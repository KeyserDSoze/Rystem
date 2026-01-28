/**
 * URL Service implementation for Remix
 * 
 * This file is an example implementation for Remix framework.
 * Copy this to your project and use it with setupSocialLogin.
 * 
 * @example
 * ```typescript
 * import { RemixUrlService } from './RemixUrlService';
 * import { useSearchParams } from '@remix-run/react';
 * 
 * const remixUrlService = new RemixUrlService();
 * 
 * setupSocialLogin(x => {
 *     x.urlService = remixUrlService;
 * });
 * 
 * export default function LoginRoute() {
 *     const [searchParams] = useSearchParams();
 *     useEffect(() => {
 *         remixUrlService.initialize(() => searchParams);
 *     }, [searchParams]);
 *     return <div>Your login route</div>;
 * }
 * ```
 */

import type { IUrlService } from './IUrlService';

/**
 * URL Service for Remix framework
 * Uses @remix-run/react useSearchParams hook to read URL parameters
 */
export class RemixUrlService implements IUrlService {
    private searchParamsGetter: (() => URLSearchParams) | null = null;

    /**
     * Initialize with useSearchParams hook from @remix-run/react
     * MUST be called inside a Remix route component
     * 
     * @param searchParamsGetter - Function that returns current URLSearchParams from useSearchParams hook
     * 
     * @example
     * ```typescript
     * const [searchParams] = useSearchParams();
     * useEffect(() => {
     *     remixUrlService.initialize(() => searchParams);
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
                'RemixUrlService not initialized. ' +
                'Call initialize() with useSearchParams() hook from @remix-run/react inside a route component.'
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
                'RemixUrlService not initialized. ' +
                'Call initialize() with useSearchParams() hook from @remix-run/react inside a route component.'
            );
            return new URLSearchParams();
        }
        
        return this.searchParamsGetter();
    }
}
