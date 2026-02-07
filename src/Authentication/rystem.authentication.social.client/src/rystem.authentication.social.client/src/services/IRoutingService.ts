/**
 * Unified routing service for URL parameter reading and navigation
 * Handles both OAuth callback detection and client-side routing operations
 * 
 * This service solves compatibility issues with client-side routing frameworks
 * (React Router, Next.js, Remix) where native browser APIs bypass the router.
 * 
 * @example Default (Vanilla React, no routing library)
 * ```typescript
 * const routingService = new WindowRoutingService();
 * setupSocialLogin(x => {
 *     x.routingService = routingService; // Uses window.location and window.history
 * });
 * ```
 * 
 * @example React Router
 * ```typescript
 * const routingService = new ReactRouterRoutingService();
 * setupSocialLogin(x => {
 *     x.routingService = routingService;
 * });
 * 
 * // In component:
 * const [searchParams] = useSearchParams();
 * const navigate = useNavigate();
 * const location = useLocation();
 * routingService.initialize(() => searchParams, navigate, location);
 * ```
 */
export interface IRoutingService {
    // ===== URL Parameter Reading (for OAuth callbacks) =====
    
    /**
     * Get a single URL parameter value
     * Used for reading OAuth callback parameters (code, state)
     * 
     * @param key - Parameter name (e.g., 'code', 'state')
     * @returns Parameter value or null if not found
     * 
     * @example
     * ```typescript
     * const code = routingService.getSearchParam('code');
     * const state = routingService.getSearchParam('state');
     * ```
     */
    getSearchParam(key: string): string | null;
    
    /**
     * Get all URL parameters
     * Used for debugging or advanced OAuth scenarios
     * 
     * @returns URLSearchParams object with all query parameters
     * 
     * @example
     * ```typescript
     * const allParams = routingService.getAllSearchParams();
     * console.log('OAuth callback params:', allParams.toString());
     * ```
     */
    getAllSearchParams(): URLSearchParams;
    
    // ===== Navigation Operations (for OAuth redirects and return URLs) =====
    
    /**
     * Get current path including search parameters
     * Used for saving return URL before OAuth redirect
     * 
     * @returns Current route path with query string (e.g., "/dashboard?tab=settings")
     * 
     * @example
     * ```typescript
     * // Save current location before OAuth
     * const returnUrl = routingService.getCurrentPath();
     * storageService.set('return_url', returnUrl);
     * ```
     */
    getCurrentPath(): string;
    
    /**
     * Navigate to a URL (for OAuth redirects and internal navigation)
     * 
     * @param url - Full URL (https://...) for external OAuth, or path (/login) for internal
     * 
     * @example OAuth redirect
     * ```typescript
     * routingService.navigateTo('https://login.microsoftonline.com/oauth2/authorize?...');
     * ```
     * 
     * @example Internal navigation (React Router)
     * ```typescript
     * routingService.navigateTo('/dashboard'); // Calls navigate() internally
     * ```
     */
    navigateTo(url: string): void;
    
    /**
     * Replace current URL without full navigation (cleanup after OAuth callback)
     * Updates URL bar without triggering page reload or adding to browser history
     * 
     * @param path - Path to replace current URL with
     * 
     * @example Clean URL after OAuth
     * ```typescript
     * // Before: /account/login?code=ABC123&state=microsoft
     * routingService.navigateReplace('/account/login');
     * // After: /account/login (query params removed)
     * ```
     */
    navigateReplace(path: string): void;
    
    /**
     * Open URL in popup window (for popup mode OAuth)
     * 
     * @param url - URL to open in popup
     * @param name - Window name/target
     * @param features - Window features (size, position, menubar, etc.)
     * @returns Window reference or null if blocked by popup blocker
     * 
     * @example
     * ```typescript
     * const popup = routingService.openPopup(
     *     'https://oauth.provider.com/authorize',
     *     'oauth_popup',
     *     'width=450,height=730,top=100,left=100'
     * );
     * if (!popup) {
     *     alert('Popup blocked! Please allow popups for this site.');
     * }
     * ```
     */
    openPopup(url: string, name: string, features: string): Window | null;
}
