import { CreateSocialButton, ProviderType, SocialButtonProps, getSocialLoginSettings, buildRedirectUri } from "../..";
import { AmazonLoginButton } from '../graphics/AmazonLoginButton';

const SDK_URL = 'https://assets.loginwithamazon.com/sdk/na/login1.js';
const _window = window as any;
const scope = "profile";
const token = "token";
const state = "";
const scopeData = {
    profile: { essential: true },
};

export const AmazonButton = ({ className = '', }: SocialButtonProps): JSX.Element => {
const settings = getSocialLoginSettings();
    if (settings.amazon.clientId) {
        const redirectUri = buildRedirectUri(settings);
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
        return (
            <CreateSocialButton key="a"
                provider={ProviderType.Amazon}
                className={className}
                redirect_uri={""}
                scriptUri={SDK_URL}
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