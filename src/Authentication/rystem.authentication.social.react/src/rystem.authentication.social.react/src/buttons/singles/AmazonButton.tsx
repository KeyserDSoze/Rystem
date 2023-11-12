import { CreateSocialButton, ProviderType, SocialButtonProps, getSocialLoginSettings } from "../..";
import { AmazonLoginButton } from 'react-social-login-buttons';

const JS_SRC = 'https://assets.loginwithamazon.com/sdk/na/login1.js';
const _window = window as any;
const scope = "profile";
const token = "token";
const state = "";
const scopeData = {
    profile: { essential: true },
};

export const AmazonButton = ({ className = '', }: SocialButtonProps): JSX.Element => {
    const settings = getSocialLoginSettings();
    const redirectUri = `${settings.redirectDomain}/account/login`;
    const onClick = (handleResponse: (code: string) => void, handleError: (message: string) => void) => {
        _window.amazon.Login.authorize(
            { scope, scopeData, token, redirectUri, state },
            (res: any) => {
                if (res.error)
                    handleError(res.error);
                else
                    handleResponse(res.access_token);
            },
        );
    };
    if (settings.amazon.clientId) {
        return (
            <CreateSocialButton key="a"
                provider={ProviderType.Amazon}
                className={className}
                redirect_uri={""}
                scriptUri={JS_SRC}
                onScriptLoad={() => _window.amazon.Login.setClientId(settings.amazon.clientId)}
                onClick={onClick}
            >
                <AmazonLoginButton />
            </CreateSocialButton>
        );
    }
    else {
        return (<></>);
    }
};