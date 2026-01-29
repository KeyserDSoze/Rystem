import React, { useCallback, useEffect, useRef, useState } from 'react';
import { ProviderType, SocialLoginManager, getSocialLoginSettings } from '..';
import { LoginMode } from '../models/setup/LoginMode';

interface Props {
    provider: ProviderType;
    className?: string;
    redirect_uri: string;
    children?: React.ReactNode;
    queryCode?: string;
    queryState?: string;
    scriptUri?: string;
    onScriptLoad?: () => void;
    onClick?: (handleResponse: (code: string) => void, handleError: (error: string) => void) => void;
}

const social_code_base = "social_code";

export const CreateSocialButton = ({
    provider,
    className = '',
    redirect_uri,
    children,
    queryCode = 'code',
    queryState = 'state',
    scriptUri,
    onScriptLoad,
    onClick
}: Props) => {
    const settings = getSocialLoginSettings();
    const social_code = `${social_code_base}_${provider}`;
    
    // Determine login mode from platform config (default: Popup)
    const loginMode = settings.platform?.loginMode || LoginMode.Popup;
    
    useEffect(() => {
        // Clean up any leftover popup result from previous attempts
        // Note: Popup mode uses native localStorage for cross-window communication (storage event)
        // The popup processes token exchange and saves the result (success or error)
        const social_result = `social_result_${provider}`;
        localStorage.removeItem(social_result);
    }, [provider]);
    
    useEffect(() => {
        // Note: Popup window callback (detect code + write to localStorage) 
        // is now handled by SocialLoginWrapper to ensure it works consistently
        // 
        // Note: Redirect mode callback is also handled by SocialLoginWrapper
        // to ensure it works regardless of which components are mounted
    }, [loginMode]);
    
    const [isSdkLoaded, setIsSdkLoaded] = useState(false);
    const scriptId = `script_${provider}`;
    const scriptNodeRef = useRef<HTMLScriptElement>(null!);

    useEffect(() => {
        !isSdkLoaded && load();
    }, [isSdkLoaded]);

    useEffect(
        () => () => {
            if (scriptNodeRef.current)
                scriptNodeRef.current.remove();
        },
        [],
    );

    const checkIsExistsSDKScript = useCallback(() => {
        return !!document.getElementById(scriptId);
    }, []);

    const insertScript = useCallback(
        (
            d: HTMLDocument,
            s: string = 'script',
            id: string,
            jsSrc: string,
            cb: () => void,
        ) => {
            const sdkScriptTag: any = d.createElement(s);
            sdkScriptTag.id = id;
            sdkScriptTag.src = jsSrc;
            sdkScriptTag.async = true;
            sdkScriptTag.defer = true;
            sdkScriptTag.crossorigin = "anonymous";
            const scriptNode = document.getElementsByTagName('script')![0];
            scriptNodeRef.current = sdkScriptTag;
            scriptNode &&
                scriptNode.parentNode &&
                scriptNode.parentNode.insertBefore(sdkScriptTag, scriptNode);
            sdkScriptTag.onload = cb;
        },
        [],
    );

    const load = useCallback(() => {
        if (checkIsExistsSDKScript()) {
            setIsSdkLoaded(true);
        } else {
            insertScript(document, 'script', scriptId, scriptUri ?? "", () => {
                onScriptLoad && onScriptLoad();
                setIsSdkLoaded(true);
            });
        }
    }, [checkIsExistsSDKScript, insertScript]);


    const handlePostMessage = useCallback(
        async (code: string) => {
            if (code)
                SocialLoginManager.Instance(null).updateToken(provider, code);
            else
                settings.onLoginFailure({ code: 3, message: "error clicking social button.", provider: provider });
        },
        [
            redirect_uri,
        ],
    );

    const handleError = (x: string) => {
        settings.onLoginFailure({ code: 7, message: x, provider: provider });
    }

    const onChangeLocalStorage = useCallback(() => {
        window.removeEventListener('storage', onChangeLocalStorage, false);
        // Popup mode: The popup processes the token exchange and saves the result
        // We just need to read the result and update the UI (or show error)
        const social_result = `social_result_${provider}`;
        const resultJson = localStorage.getItem(social_result);
        
        if (resultJson) {
            try {
                const result = JSON.parse(resultJson);
                localStorage.removeItem(social_result);
                
                if (result.success) {
                    console.log('✅ [Main Window] Popup login successful, refreshing UI');
                    // Token and user are already saved by the popup's updateToken call
                    // Just trigger a refresh to update the UI
                    SocialLoginManager.Instance(null).refresher();
                } else {
                    console.error('❌ [Main Window] Popup login failed:', result.error);
                    // Call error handler
                    settings.onLoginFailure({
                        code: 4,
                        message: result.error || 'Login failed in popup',
                        provider: result.provider
                    });
                }
            } catch (error) {
                console.error('❌ [Main Window] Failed to parse popup result:', error);
                settings.onLoginFailure({
                    code: 5,
                    message: 'Failed to parse login result from popup',
                    provider: provider
                });
            }
        }
    }, [provider]);

    const onLogin = useCallback(() => {
        if (onClick) {
            if (!isSdkLoaded) {
                load();
            } else {
                onClick(handlePostMessage, handleError);
            }
        } else {
            // Check login mode
            if (loginMode === LoginMode.Redirect) {
                // Redirect mode: save current URL to return after OAuth callback
                const currentPath = settings.routingService.getCurrentPath();
                settings.storageService.set('social_login_return_url', currentPath);
                console.log('💾 Saved return URL before OAuth redirect:', currentPath);

                // Navigate to OAuth URL using routing service
                settings.routingService.navigateTo(redirect_uri);
            } else {
                // Popup mode: open in popup window using routing service
                window.addEventListener('storage', onChangeLocalStorage, false);
                const width = 450;
                const height = 730;
                const left = window.screen.width / 2 - width / 2;
                const top = window.screen.height / 2 - height / 2;
                const features = 'menubar=no,location=no,resizable=no,scrollbars=no,status=no, width=' +
                    width +
                    ', height=' +
                    height +
                    ', top=' +
                    top +
                    ', left=' +
                    left;
                settings.routingService.openPopup(
                    redirect_uri,
                    provider.toString(),
                    features
                );
            }
        }
    }, [
        redirect_uri,
        isSdkLoaded,
        onChangeLocalStorage,
        loginMode,
    ]);

    return (
        <div className={className} key={provider.toString()} onClick={onLogin}>
            {children}
        </div>
    );
};