/**
 * Unified Routing Service for Next.js App Router (v13+)
 * 
 * This service handles both URL parameter reading and navigation for Next.js with App Router.
 * Copy this file to your project and remove the `.example` extension.
 * 
 * IMPORTANT: This must be used in Client Components ('use client')
 * 
 * @example
 * ```typescript
 * 'use client';
 * 
 * import { NextAppRouterRoutingService } from './NextAppRouterRoutingService';
 * import { useRouter, usePathname, useSearchParams } from 'next/navigation';
 * 
 * const routingService = new NextAppRouterRoutingService();
 * 
 * setupSocialLogin(x => {
 *     x.routingService = routingService;
 * });
 * 
 * export default function LoginPage() {
 *     const router = useRouter();
 *     const pathname = usePathname();
 *     const searchParams = useSearchParams();
 *     
 *     useEffect(() => {
 *         // Single initialization with all hooks
 *         routingService.initialize(router, pathname, searchParams);
 *     }, [router, pathname, searchParams]);
 *     
 *     return <div>Your login page</div>;
 * }
 * ```
 */

import type { IRoutingService } from './IRoutingService';

/**
 * Routing Service for Next.js App Router (v13+)
 * Uses useRouter, usePathname, and useSearchParams from next/navigation
 * 
 * NOTE: Only works in Client Components ('use client' directive required)
 */
export class NextAppRouterRoutingService implements IRoutingService {
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
     *     routingService.initialize(router, pathname, searchParams);
     * }, [router, pathname, searchParams]);
     * ```
     */
    initialize(router: any, pathname: string, searchParams: URLSearchParams | null): void {
        this.router = router;
        this.pathname = pathname;
        this.searchParams = searchParams;
    }

    // ===== URL Parameter Reading =====

    getSearchParam(key: string): string | null {
        if (!this.searchParams) {
            console.warn('NextAppRouterRoutingService not initialized. Call initialize() with Next.js hooks.');
            return null;
        }
        return this.searchParams.get(key);
    }

    getAllSearchParams(): URLSearchParams {
        if (!this.searchParams) {
            console.warn('NextAppRouterRoutingService not initialized. Call initialize() with Next.js hooks.');
            return new URLSearchParams();
        }
        return this.searchParams;
    }

    // ===== Navigation Operations =====

    getCurrentPath(): string {
        if (!this.pathname) {
            console.warn('NextAppRouterRoutingService not initialized. Falling back to window.location.');
            return window.location.pathname + window.location.search;
        }
        
        const search = this.searchParams?.toString();
        return search ? `${this.pathname}?${search}` : this.pathname;
    }

    navigateTo(url: string): void {
        // External OAuth redirects (https://...) must use window.location
        if (url.startsWith('http://') || url.startsWith('https://')) {
            console.log('🌐 [NextJS] External OAuth redirect, using window.location:', url);
            window.location.href = url;
        } else if (this.router) {
            console.log('🔄 [NextJS] Internal navigation:', url);
            this.router.push(url);
        } else {
            console.warn('NextAppRouterRoutingService not initialized. Using window.location.');
            window.location.href = url;
        }
    }

    navigateReplace(path: string): void {
        if (this.router) {
            console.log('🔄 [NextJS] Replacing URL:', path);
            this.router.replace(path);
        } else {
            console.warn('NextAppRouterRoutingService not initialized. Using window.history.');
            window.history.replaceState({}, '', path);
        }
    }

    openPopup(url: string, name: string, features: string): Window | null {
        return window.open(url, name, features);
    }
}
