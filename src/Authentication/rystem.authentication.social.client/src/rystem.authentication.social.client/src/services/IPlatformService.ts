/**
 * Platform-agnostic service for environment-specific operations
 * Abstracts browser/native APIs for cross-platform support
 * 
 * Implementations:
 * - BrowserPlatformService (web): Uses window, document, storage events
 * - ReactNativePlatformService (mobile): Uses EventEmitter, Dimensions, Linking
 * 
 * Use cases:
 * - Web: DOM manipulation, popup windows, storage events
 * - React Native: Custom events, screen dimensions, deep links
 * - SSR: No-op implementations to avoid crashes
 */
export interface IPlatformService {
    // ===== Storage Event Handling (for popup communication) =====
    
    /**
     * Add listener for storage changes (used for popup → main window communication)
     * Web: Uses window.addEventListener('storage', ...)
     * React Native: Uses custom event emitter or no-op
     * 
     * @param callback Function to call when storage changes
     */
    addStorageListener(callback: () => void): void;
    
    /**
     * Remove storage event listener
     * Web: Uses window.removeEventListener('storage', ...)
     * React Native: Removes custom event listener
     * 
     * @param callback Function to remove
     */
    removeStorageListener(callback: () => void): void;
    
    // ===== Screen Dimensions (for popup positioning) =====
    
    /**
     * Get screen width for popup positioning
     * Web: Returns window.screen.width
     * React Native: Returns Dimensions.get('window').width
     * 
     * @returns Screen width in pixels
     */
    getScreenWidth(): number;
    
    /**
     * Get screen height for popup positioning
     * Web: Returns window.screen.height
     * React Native: Returns Dimensions.get('window').height
     * 
     * @returns Screen height in pixels
     */
    getScreenHeight(): number;
    
    // ===== Script Loading (for external SDKs like Google, Facebook) =====
    
    /**
     * Load external JavaScript SDK
     * Web: Injects <script> tag into document
     * React Native: No-op (SDKs are bundled or not used)
     * 
     * @param id Unique script ID
     * @param src Script URL
     * @param onLoad Callback when script loads
     * @returns Script element if loaded, null otherwise
     */
    loadScript(id: string, src: string, onLoad: () => void): HTMLScriptElement | null;
    
    /**
     * Check if script is already loaded
     * Web: Checks document.getElementById(id)
     * React Native: Returns false (no script loading)
     * 
     * @param id Script ID
     * @returns True if script exists
     */
    scriptExists(id: string): boolean;
    
    /**
     * Remove loaded script
     * Web: Removes script tag from DOM
     * React Native: No-op
     * 
     * @param scriptElement Script element to remove
     */
    removeScript(scriptElement: HTMLScriptElement): void;

    // ===== Window Operations (for popup detection and closing) =====

    /**
     * Check if current window is a popup window
     * Web: Checks window.opener !== null
     * React Native: Returns false (no popup concept)
     * 
     * @returns True if running in a popup window
     */
    isPopup(): boolean;

    /**
     * Close current window (used after popup OAuth callback)
     * Web: Calls window.close()
     * React Native: No-op or custom navigation
     */
    closeWindow(): void;
}
