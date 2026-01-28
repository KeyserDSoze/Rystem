/**
 * URL Service implementation for Next.js App Router (v13+)
 * 
 * This file is an example implementation for Next.js with App Router.
 * Copy this to your project and use it with setupSocialLogin.
 * 
 * IMPORTANT: This must be used in Client Components ('use client')
 * 
 * @example
 * ```typescript
 * 'use client';
 * 
 * import { NextAppRouterUrlService } from './NextAppRouterUrlService';
 * import { useSearchParams } from 'next/navigation';
 * 
 * const nextUrlService = new NextAppRouterUrlService();
 * 
 * setupSocialLogin(x => {
 *     x.urlService = nextUrlService;
 * });
 * 
 * export default function LoginPage() {
 *     const searchParams = useSearchParams();
 *     useEffect(() => {
 *         nextUrlService.initialize(() => searchParams);
 *     }, [searchParams]);
 *     return <div>Your login page</div>;
 * }
 * ```
 */

import type { IUrlService } from './IUrlService';

/**
 * URL Service for Next.js App Router (v13+)
 * Uses next/navigation useSearchParams hook to read URL parameters
 * 
 * NOTE: Only works in Client Components ('use client' directive required)
 */
export class NextAppRouterUrlService implements IUrlService {
    private searchParamsGetter: (() => URLSearchParams | null) | null = null;

    /**
     * Initialize with useSearchParams hook from next/navigation
     * MUST be called inside a Client Component ('use client')
     * 
     * @param searchParamsGetter - Function that returns current URLSearchParams from useSearchParams hook
     * 
     * @example
     * ```typescript
     * 'use client';
     * 
     * const searchParams = useSearchParams();
     * useEffect(() => {
     *     nextUrlService.initialize(() => searchParams);
     * }, [searchParams]);
     * ```
     */
    initialize(searchParamsGetter: () => URLSearchParams | null): void {
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
                'NextAppRouterUrlService not initialized. ' +
                'Call initialize() with useSearchParams() hook from next/navigation inside a Client Component.'
            );
            return null;
        }
        
        const params = this.searchParamsGetter();
        if (!params) {
            return null;
        }
        
        return params.get(key);
    }

    /**
     * Get all URL parameters
     * @returns URLSearchParams object with all parameters (or empty if not initialized)
     */
    getAllSearchParams(): URLSearchParams {
        if (!this.searchParamsGetter) {
            console.warn(
                'NextAppRouterUrlService not initialized. ' +
                'Call initialize() with useSearchParams() hook from next/navigation inside a Client Component.'
            );
            return new URLSearchParams();
        }
        
        const params = this.searchParamsGetter();
        return params || new URLSearchParams();
    }
}
