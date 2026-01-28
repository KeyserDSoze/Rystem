import { useReducer, useEffect, useRef } from "react";
import { useSocialToken, SocialLoginContextLogout, SocialLoginContextRefresh, SocialLoginContextUpdate, removeSocialLogin, SocialLoginManager, getSocialLoginSettings } from "..";
import { ProviderType } from "../models/setup/ProviderType";
import { LoginMode } from "../models/setup/LoginMode";

const queryCode = 'code';
const queryState = 'state';

const forceRefresh = () => {
    const oldToken = useSocialToken();
    SocialLoginManager.Instance(null).updateToken(0, oldToken.refreshToken);
};

export const SocialLoginWrapper = (c: { children: any; }) => {
    const [renderingKey, forceUpdate] = useReducer(x => x + 1, 0);
    SocialLoginManager.Instance(null).refresher = () => forceUpdate();
    
    // ✅ Protection flag to prevent double processing (React.StrictMode in dev)
    const callbackProcessedRef = useRef(false);
    
    // ✅ Process OAuth callback (both Redirect and Popup modes)
    useEffect(() => {
        const settings = getSocialLoginSettings();
        const loginMode = settings.platform?.loginMode || LoginMode.Popup;
        
        const urlParams = new URLSearchParams(window.location.search);
        const code = urlParams.get(queryCode);
        const stateParam = urlParams.get(queryState);
        
        console.log('🔍 SocialLoginWrapper: OAuth callback detection:', { 
            code: code ? 'present' : 'missing', 
            state: stateParam, 
            loginMode,
            isPopup: window.opener !== null,
            alreadyProcessed: callbackProcessedRef.current
        });
        
        // ⚠️ Prevent double processing (React.StrictMode causes double mount in dev)
        if (callbackProcessedRef.current) {
            console.log('⚠️ Callback already processed, skipping to prevent duplicate API call');
            return;
        }
        
        if (code && stateParam) {
            const providerType = parseInt(stateParam);
            
            if (!isNaN(providerType) && ProviderType[providerType]) {
                const providerName = ProviderType[providerType];
                
                // Mark as processed BEFORE making API call
                callbackProcessedRef.current = true;
                
                // Check if we're in a popup window
                if (window.opener && loginMode === LoginMode.Popup) {
                    // POPUP MODE: Write to localStorage and close popup
                    console.log('✅ [Popup] Callback in popup window, saving to localStorage and closing');
                    const social_code = `social_code_${providerType}`;
                    localStorage.setItem(social_code, code);
                    window.close();
                    // Note: The main window's CreateSocialButton will receive the storage event
                    // and call handlePostMessage to process the token
                } else {
                    // REDIRECT MODE: Process token exchange directly
                    console.log('✅ [Redirect] Callback in main window, processing token exchange');
                    
                    SocialLoginManager.Instance(null).updateToken(providerType, code)
                        .then(() => {
                            console.log('✅ Token exchange successful');
                            
                            // Get return URL saved before OAuth redirect using storage service
                            const storageService = settings.storageService;
                            const returnUrl = storageService.get('social_login_return_url');
                            storageService.remove('social_login_return_url');
                            
                            if (returnUrl) {
                                console.log('🔙 Redirecting to saved return URL:', returnUrl);
                                // Navigate to saved URL
                                window.history.replaceState({}, '', returnUrl);
                            } else {
                                console.log('🏠 No return URL found, staying on callback page');
                                // Clean URL (remove query params)
                                const newUrl = window.location.pathname;
                                window.history.replaceState({}, '', newUrl);
                            }
                            
                            forceUpdate();
                        })
                        .catch((error) => {
                            console.error('❌ Token exchange failed:', error);
                            // Reset flag on error to allow retry
                            callbackProcessedRef.current = false;
                            
                            if (settings.onLoginFailure) {
                                settings.onLoginFailure({
                                    message: error.message || 'Token exchange failed',
                                    provider: providerName
                                } as any);
                            }
                            
                            // Clean return URL on error using storage service
                            settings.storageService.remove('social_login_return_url');
                            
                            // Clean URL even on error
                            const newUrl = window.location.pathname;
                            window.history.replaceState({}, '', newUrl);
                        });
                }
            } else {
                console.warn('⚠️ Invalid provider state:', stateParam);
            }
        }
    }, []);
    
    const forceLogout = () => {
        removeSocialLogin();
        forceUpdate();
    };
    
    return (
        <div key={renderingKey}>
            <SocialLoginContextUpdate.Provider value={forceUpdate}>
                <SocialLoginContextRefresh.Provider value={forceRefresh}>
                    <SocialLoginContextLogout.Provider value={forceLogout}>
                        {c.children}
                    </SocialLoginContextLogout.Provider>
                </SocialLoginContextRefresh.Provider>
            </SocialLoginContextUpdate.Provider>
        </div>
    );
};
