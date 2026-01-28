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

        // Use urlService to read URL parameters (supports React Router, Next.js, etc.)
        const urlService = settings.urlService;
        const code = urlService.getSearchParam(queryCode);
        const stateParam = urlService.getSearchParam(queryState);

        console.log('🔍 SocialLoginWrapper: OAuth callback detection:', { 
            code: code ? 'present' : 'missing', 
            state: stateParam, 
            loginMode,
            isPopup: window.opener !== null,
            alreadyProcessed: callbackProcessedRef.current,
            urlService: urlService.constructor.name
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
                
                // Check if we're in a popup window (simpler condition)
                // If window.opener exists, we're definitely in a popup, regardless of loginMode config
                if (window.opener) {
                    // POPUP MODE: Process token exchange IN THE POPUP, then save result and close
                    console.log('✅ [Popup] Detected popup window, processing token exchange in popup');
                    const social_code = `social_code_${providerType}`;
                    const social_result = `social_result_${providerType}`;
                    
                    SocialLoginManager.Instance(null).updateToken(providerType, code)
                        .then(() => {
                            console.log('✅ [Popup] Token exchange successful, saving success result');
                            
                            // Save success result for main window
                            localStorage.setItem(social_result, JSON.stringify({
                                success: true,
                                provider: providerName
                            }));
                            
                            // Close popup after ensuring localStorage is written
                            setTimeout(() => {
                                console.log('🔒 [Popup] Closing popup after successful login');
                                window.close();
                            }, 100);
                        })
                        .catch((error) => {
                            console.error('❌ [Popup] Token exchange failed:', error);
                            
                            // Save error result for main window
                            localStorage.setItem(social_result, JSON.stringify({
                                success: false,
                                provider: providerName,
                                error: error.message || 'Token exchange failed'
                            }));
                            
                            // Close popup after ensuring localStorage is written
                            setTimeout(() => {
                                console.log('🔒 [Popup] Closing popup after error');
                                window.close();
                            }, 100);
                        });
                    
                    // Note: The main window's CreateSocialButton will receive the storage event
                    // and read the result (success or error) from localStorage
                } else {
                    // REDIRECT MODE: Process token exchange directly in main window
                    console.log('✅ [Redirect] Main window detected, processing token exchange');

                    SocialLoginManager.Instance(null).updateToken(providerType, code)
                        .then(() => {
                            console.log('✅ Token exchange successful');

                            // Get return URL saved before OAuth redirect using storage service
                            const storageService = settings.storageService;
                            const returnUrl = storageService.get('social_login_return_url');
                            storageService.remove('social_login_return_url');

                            if (returnUrl) {
                                console.log('🔙 Navigating to saved return URL:', returnUrl);
                                // Navigate to saved URL using navigation service
                                settings.navigationService.navigateReplace(returnUrl);
                            } else {
                                console.log('🏠 No return URL found, cleaning callback URL');
                                // Clean URL (remove query params) using navigation service
                                const cleanPath = settings.navigationService.getCurrentPath().split('?')[0];
                                settings.navigationService.navigateReplace(cleanPath);
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

                            // Clean URL even on error using navigation service
                            const cleanPath = settings.navigationService.getCurrentPath().split('?')[0];
                            settings.navigationService.navigateReplace(cleanPath);
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
