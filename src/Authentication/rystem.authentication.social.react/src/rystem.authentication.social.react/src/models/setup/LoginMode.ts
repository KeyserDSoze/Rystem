/**
 * Login interaction mode
 */
export enum LoginMode {
    /**
     * Open OAuth in a popup window (default for web)
     */
    Popup = 'popup',
    
    /**
     * Redirect to OAuth provider (default for mobile)
     */
    Redirect = 'redirect'
}
