import React, { useCallback, useEffect, useRef, useState } from 'react';
import { ProviderType, SocialLoginManager, getSocialLoginSettings } from '..';

interface Props {
    provider: ProviderType;
    className?: string;
    redirect_uri: string;
    children?: React.ReactNode;
    queryCode?: string;
    scriptUri?: string;
    onScriptLoad?: () => void;
    onClick?: (handleResponse: (code: string) => void, handleError: (error: string) => void) => void;
}

const social_code = "social_code";

export const CreateSocialButton = ({
    provider,
    className = '',
    redirect_uri,
    children,
    queryCode = 'code',
    scriptUri,
    onScriptLoad,
    onClick
}: Props) => {
    const settings = getSocialLoginSettings();
    useEffect(() => {
        const popupWindowURL = new URL(window.location.href);
        const code = popupWindowURL.searchParams.get(queryCode);
        if (code) {
            localStorage.setItem(social_code, code);
            window.close();
        }
    }, []);
    const [isSdkLoaded, setIsSdkLoaded] = useState(false);
    const scriptId = `script_${provider}`;
    const scriptNodeRef = useRef<HTMLScriptElement>(null!);

    useEffect(() => {
        !isSdkLoaded && load();
    }, [isSdkLoaded]);

    useEffect(
        () => () => {
            if (scriptNodeRef.current) scriptNodeRef.current.remove();
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
    }, [
        redirect_uri,
        isSdkLoaded,
        onChangeLocalStorage,
    ]);

    return (
        <div className={className} key={provider.toString()} onClick={onLogin}>
            {children}
        </div>
    );
};