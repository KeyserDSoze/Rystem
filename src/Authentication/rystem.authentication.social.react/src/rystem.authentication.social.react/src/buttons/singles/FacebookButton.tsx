import { CreateSocialButton, ProviderType, SocialButtonProps, getSocialLoginSettings } from "../..";
import { FacebookLoginButton } from 'react-social-login-buttons';

const SDK_URL: string = 'https://connect.facebook.net/en_EN/sdk.js';
const _window: any = window;

export const FacebookButton = ({ className = '', }: SocialButtonProps): JSX.Element => {
    const settings = getSocialLoginSettings();
    if (settings.facebook.clientId) {
        const redirectUri = `${settings.redirectDomain}/account/login`;
        const scope = "email,public_profile";
        const return_scopes = true;
        const auth_type = "";
        const onClick = (handleResponse: (code: string) => void, handleError: (message: string) => void) => {
            _window.FB.login((response: any) => {
                if (response.authResponse) {
                    handleResponse(response.authResponse.accessToken);
                }
                else
                    handleError("error");
            },
                {
                    scope,
                    return_scopes,
                    auth_type,
                });
        };
        const config = {
            appId: settings.facebook.clientId,
            xfbml: true,
            version: "v2.7",
            state: true,
            cookie: false,
            redirect_uri: redirectUri,
            response_type: "code",
        };
        const onInit = () => {
            _window.fbAsyncInit = function () {
                _window.FB && _window.FB.init({ ...config });
                let fbRoot = document.getElementById('fb-root');
                if (!fbRoot) {
                    fbRoot = document.createElement('div');
                    fbRoot.id = 'fb-root';
                    document.body.appendChild(fbRoot);
                }
            }
        }
        return (
            <CreateSocialButton key="f"
                provider={ProviderType.Facebook}
                className={className}
                redirect_uri={""}
                scriptUri={SDK_URL}
                onScriptLoad={onInit}
                onClick={onClick}
            >
                <FacebookLoginButton />
            </CreateSocialButton>
        );
    }
    else {
        return (<></>);
    }
};
