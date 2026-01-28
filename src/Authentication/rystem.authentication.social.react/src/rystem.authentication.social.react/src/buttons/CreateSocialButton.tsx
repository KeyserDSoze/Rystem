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
        localStorage
            .removeItem(social_code);
    }, []);
    
    useEffect(() => {
        // For popup mode: handle popup window callback
        if (loginMode === LoginMode.Popup) {
            const popupWindowURL = new URL(window.location.href);
            const code = popupWindowURL.searchParams.get(queryCode);
            const state = popupWindowURL.searchParams.get(queryState);
            if (code && state && parseInt(state) == provider) {
                localStorage.setItem(social_code, code);
                window.close();
            }
        }
        
        // For redirect mode: handle redirect callback
        if (loginMode === LoginMode.Redirect) {
            const currentUrl = new URL(window.location.href);
            const code = currentUrl.searchParams.get(queryCode);
            const state = currentUrl.searchParams.get(queryState);
            if (code && state && parseInt(state) == provider) {
                handlePostMessage(code);
                // Clean URL after processing
                const cleanUrl = new URL(window.location.href);
                cleanUrl.searchParams.delete(queryCode);
                cleanUrl.searchParams.delete(queryState);
                window.history.replaceState({}, document.title, cleanUrl.toString());
            }
        }
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
        const code = localStorage.getItem(social_code);
        if (code) {
            handlePostMessage(code);
            localStorage.removeItem(social_code);
        }
    }, [handlePostMessage]);

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
                // Redirect mode: navigate to OAuth URL directly
                window.location.href = redirect_uri;
            } else {
                // Popup mode: open in popup window
                window.addEventListener('storage', onChangeLocalStorage, false);
                const width = 450;
                const height = 730;
                const left = window.screen.width / 2 - width / 2;
                const top = window.screen.height / 2 - height / 2;
                window.open(
                    redirect_uri,
                    provider.toString(),
                    'menubar=no,location=no,resizable=no,scrollbars=no,status=no, width=' +
                    width +
                    ', height=' +
                    height +
                    ', top=' +
                    top +
                    ', left=' +
                    left,
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