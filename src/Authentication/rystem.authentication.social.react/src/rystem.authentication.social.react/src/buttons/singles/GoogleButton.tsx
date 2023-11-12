import { ProviderType, SocialLoginManager, getSocialLoginSettings } from "../..";
import { LoginSocialGoogle } from 'reactjs-social-login';
import { GoogleLoginButton } from 'react-social-login-buttons';

export const GoogleButton = (): JSX.Element => {
    const settings = getSocialLoginSettings();
    const redirectUri = `${settings.redirectDomain}/account/login`;
    return (
        <div key="g">
            {settings.google.clientId != null &&
                <LoginSocialGoogle
                    client_id={settings.google.clientId}
                    redirect_uri={redirectUri}
                    scope="openid profile email"
                    discoveryDocs="claims_supported"
                    access_type="offline"
                    isOnlyGetToken={true}
                    onResolve={(x: any) => {
                        SocialLoginManager.Instance(null).updateToken(ProviderType.Google, x.data?.code);
                    }}
                    onReject={() => {
                        settings.onLoginFailure({ code: 3, message: "error clicking social button.", provider: ProviderType.Google });
                    }}>
                    <GoogleLoginButton />
                </LoginSocialGoogle>}
        </div>
    );
};
