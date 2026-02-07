import type { IPlatformService } from './IPlatformService';

/**
 * Browser implementation of IPlatformService
 * Uses native window, document, and DOM APIs
 * 
 * @example
 * ```typescript
 * const platformService = new BrowserPlatformService();
 * 
 * // Listen for storage events (popup communication)
 * platformService.addStorageListener(() => {
 *     console.log('Storage changed!');
 * });
 * 
 * // Get screen dimensions
 * const width = platformService.getScreenWidth();
 * 
 * // Load external script (e.g., Google SDK)
 * platformService.loadScript('google-sdk', 'https://apis.google.com/js/api.js', () => {
 *     console.log('Google SDK loaded!');
 * });
 * ```
 */
export class BrowserPlatformService implements IPlatformService {
    /**
     * Check if browser APIs are available
     */
    private isAvailable(): boolean {
        return typeof window !== 'undefined';
    }
    
    // ===== Storage Event Handling =====
    
    addStorageListener(callback: () => void): void {
        if (!this.isAvailable()) {
            console.warn('BrowserPlatformService: window not available (SSR?)');
            return;
        }
        window.addEventListener('storage', callback, false);
    }
    
    removeStorageListener(callback: () => void): void {
        if (!this.isAvailable()) {
            return;
        }
        window.removeEventListener('storage', callback, false);
    }
    
    // ===== Screen Dimensions =====
    
    getScreenWidth(): number {
        if (!this.isAvailable() || !window.screen) {
            return 1024; // Default fallback
        }
        return window.screen.width;
    }
    
    getScreenHeight(): number {
        if (!this.isAvailable() || !window.screen) {
            return 768; // Default fallback
        }
        return window.screen.height;
    }
    
    // ===== Script Loading =====
    
    loadScript(id: string, src: string, onLoad: () => void): HTMLScriptElement | null {
        if (!this.isAvailable() || typeof document === 'undefined') {
            console.warn('BrowserPlatformService: document not available (SSR?)');
            onLoad(); // Call callback anyway to avoid hanging
            return null;
        }

        // Check if script already exists
        if (this.scriptExists(id)) {
            onLoad();
            return document.getElementById(id) as HTMLScriptElement;
        }

        const script = document.createElement('script');
        script.id = id;
        script.src = src;
        script.async = true;
        script.defer = true;
        script.crossOrigin = 'anonymous';
        script.onload = onLoad;

        const firstScript = document.getElementsByTagName('script')[0];
        if (firstScript && firstScript.parentNode) {
            firstScript.parentNode.insertBefore(script, firstScript);
        } else {
            document.head.appendChild(script);
        }

        return script;
    }
    
    scriptExists(id: string): boolean {
        if (!this.isAvailable() || typeof document === 'undefined') {
            return false;
        }
        return !!document.getElementById(id);
    }
    
    removeScript(scriptElement: HTMLScriptElement): void {
        if (!this.isAvailable() || typeof document === 'undefined') {
            return;
        }
        if (scriptElement && scriptElement.parentNode) {
            scriptElement.parentNode.removeChild(scriptElement);
        }
    }

    // ===== Window Operations =====

    isPopup(): boolean {
        if (!this.isAvailable()) {
            return false;
        }
        return window.opener !== null;
    }

    closeWindow(): void {
        if (!this.isAvailable()) {
            console.warn('BrowserPlatformService: window not available (SSR?)');
            return;
        }
        window.close();
    }
}
