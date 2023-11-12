import { ProviderType, SocialLoginManager, getSocialLoginSettings } from "../..";
import { LoginSocialMicrosoft } from 'reactjs-social-login';
import { MicrosoftLoginButton } from 'react-social-login-buttons';

export const MicrosoftButton = (): JSX.Element => {
    const settings = getSocialLoginSettings();
    const redirectUri = `${settings.redirectDomain}/account/login`;
    return (
        <div key="m">
            {settings.microsoft.clientId &&
                <LoginSocialMicrosoft
                    client_id={settings.microsoft.clientId}
                    tenant="consumers"
                    redirect_uri={redirectUri}
                    onResolve={(x: any) => {
                        SocialLoginManager.Instance(null).updateToken(ProviderType.Microsoft, x.data?.id_token);
                    }}
                    onReject={() => {
                        settings.onLoginFailure({ code: 3, message: "error clicking social button.", provider: ProviderType.Microsoft });
                    }}
                    isOnlyGetToken={true}
                >
                    <MicrosoftLoginButton />
                </LoginSocialMicrosoft>}
        </div>
    );
};
