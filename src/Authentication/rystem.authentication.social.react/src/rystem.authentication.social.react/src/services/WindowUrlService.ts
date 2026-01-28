import { IUrlService } from "./IUrlService";

/**
 * Default URL service implementation using window.location
 * 
 * Works with:
 * - Vanilla React
 * - Next.js (App Router and Pages Router)
 * - Remix
 * - Any framework that uses standard browser navigation
 * 
 * @example
 * const urlService = new WindowUrlService();
 * const code = urlService.getSearchParam('code');
 */
export class WindowUrlService implements IUrlService {
    /**
     * Get a URL parameter from window.location.search
     */
    getSearchParam(key: string): string | null {
        const params = new URLSearchParams(window.location.search);
        return params.get(key);
    }
    
    /**
     * Get all URL parameters from window.location.search
     */
    getAllSearchParams(): URLSearchParams {
        return new URLSearchParams(window.location.search);
    }
}
