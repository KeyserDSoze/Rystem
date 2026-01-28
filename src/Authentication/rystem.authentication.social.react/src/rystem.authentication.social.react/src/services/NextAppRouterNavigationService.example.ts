/**
 * Navigation Service implementation for Next.js App Router (v13+)
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
 * import { NextAppRouterNavigationService } from './NextAppRouterNavigationService';
 * import { useRouter, usePathname, useSearchParams } from 'next/navigation';
 * 
 * const nextNavService = new NextAppRouterNavigationService();
 * 
 * setupSocialLogin(x => {
 *     x.navigationService = nextNavService;
 * });
 * 
 * export default function LoginPage() {
 *     const router = useRouter();
 *     const pathname = usePathname();
 *     const searchParams = useSearchParams();
 *     
 *     useEffect(() => {
 *         nextNavService.initialize(router, pathname, searchParams);
 *     }, [router, pathname, searchParams]);
 *     
 *     return <div>Your login page</div>;
 * }
 * ```
 */

import type { INavigationService } from './INavigationService';

/**
 * Navigation Service for Next.js App Router (v13+)
 * Uses next/navigation hooks for client-side routing
 * 
 * NOTE: Only works in Client Components ('use client' directive required)
 */
export class NextAppRouterNavigationService implements INavigationService {
    private router: any = null;
    private pathname: string | null = null;
    private searchParams: URLSearchParams | null = null;

    /**
     * Initialize with Next.js navigation hooks
     * MUST be called inside a Client Component ('use client')
     * 
     * @param router - The router from useRouter() hook
     * @param pathname - The pathname from usePathname() hook
     * @param searchParams - The searchParams from useSearchParams() hook
     * 
     * @example
     * ```typescript
     * 'use client';
     * 
     * const router = useRouter();
     * const pathname = usePathname();
     * const searchParams = useSearchParams();
     * 
     * useEffect(() => {
     *     nextNavService.initialize(router, pathname, searchParams);
     * }, [router, pathname, searchParams]);
     * ```
     */
    initialize(router: any, pathname: string, searchParams: URLSearchParams): void {
        this.router = router;
        this.pathname = pathname;
        this.searchParams = searchParams;
    }

    /**
     * Get current path from Next.js pathname and searchParams
     * @returns Current route path with query parameters
     */
    getCurrentPath(): string {
        if (!this.pathname) {
            console.warn('NextAppRouterNavigationService not initialized. Falling back to window.location.');
            return window.location.pathname + window.location.search;
        }
        
        const search = this.searchParams?.toString();
        return search ? `${this.pathname}?${search}` : this.pathname;
    }

    /**
     * Navigate to URL using Next.js router or window.location for external URLs
     * @param url - Full URL (external OAuth) or path (internal navigation)
     */
    navigateTo(url: string): void {
        // For external OAuth redirects, we must use window.location
        if (url.startsWith('http://') || url.startsWith('https://')) {
            console.log('🌐 [NextJS] External URL detected, using window.location:', url);
            window.location.href = url;
        } else if (this.router) {
            console.log('🔄 [NextJS] Internal navigation:', url);
            this.router.push(url);
        } else {
            console.warn('NextAppRouterNavigationService not initialized. Using window.location.');
            window.location.href = url;
        }
    }

    /**
     * Replace current URL using Next.js router.replace
     * @param path - Path to replace current URL with
     */
    navigateReplace(path: string): void {
        if (this.router) {
            console.log('🔄 [NextJS] Replacing URL:', path);
            this.router.replace(path);
        } else {
            console.warn('NextAppRouterNavigationService not initialized. Using window.history.');
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
