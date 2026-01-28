import { CreateSocialButton, ProviderType, SocialButtonProps, getSocialLoginSettings, buildRedirectUri } from "../..";
import { GoogleLoginButton } from "../graphics/GoogleLoginButton";

const SDK_URL: string = "https://accounts.google.com/gsi/client";
const _window = window as any;

export const GoogleButton = ({ className = '', }: SocialButtonProps): JSX.Element => {
const settings = getSocialLoginSettings();
if (settings.google.clientId) {
    const redirectUri = buildRedirectUri(settings);
    const onClick = (handleResponse: (code: string) => void, handleError: (message: string) => void) => {
            const params = {
                client_id: settings.google.clientId,
                ux_mode: "",
            };
            var client: any = null!;
            const payload = {
                ...params,
                scope: "openid profile email",
                prompt: "select_account",
                login_hint: "",
                access_type: "offline",
                hosted_domain: "",
                redirect_uri: redirectUri,
                cookie_policy: "single_host_origin",
                discoveryDocs: "",
                immediate: true,
                fetch_basic_profile: true,
                callback: (x: any) => handleResponse(x.code),
                error_callback: (x: any) => handleError(x),
            };
            client = _window.google.accounts.oauth2.initCodeClient(payload);
            if (client != null)
                client.requestCode();
        }
        return (
            <CreateSocialButton key="g"
                provider={ProviderType.Google}
                className={className}
                redirect_uri={""}
                scriptUri={SDK_URL}
                onClick={onClick}
            >
                <GoogleLoginButton />
            </CreateSocialButton>
        );
    }
    else {
        return (<></>);
    }
};
