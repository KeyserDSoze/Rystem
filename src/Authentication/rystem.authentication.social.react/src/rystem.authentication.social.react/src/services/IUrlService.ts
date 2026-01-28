/**
 * Service abstraction for reading URL parameters
 * 
 * This allows supporting different routing libraries (React Router, Next.js, etc.)
 * while keeping the OAuth callback logic framework-agnostic.
 * 
 * @example Default (window.location)
 * const urlService = new WindowUrlService();
 * 
 * @example React Router (custom implementation)
 * class ReactRouterUrlService implements IUrlService {
 *   getSearchParam(key: string): string | null {
 *     const [searchParams] = useSearchParams();
 *     return searchParams.get(key);
 *   }
 * }
 */
export interface IUrlService {
    /**
     * Get a single URL search parameter by key
     * @param key The parameter name (e.g., "code", "state")
     * @returns The parameter value or null if not found
     */
    getSearchParam(key: string): string | null;
    
    /**
     * Get all URL search parameters
     * @returns URLSearchParams object with all parameters
     */
    getAllSearchParams(): URLSearchParams;
}
