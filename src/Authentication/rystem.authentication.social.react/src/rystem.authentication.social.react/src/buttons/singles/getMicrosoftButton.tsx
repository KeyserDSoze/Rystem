import { ProviderType, SocialButtonValue, SocialLoginManager, getSocialLoginSettings } from "../..";
import { LoginSocialMicrosoft } from 'reactjs-social-login';
import { MicrosoftLoginButton } from 'react-social-login-buttons';

export const getMicrosoftButton = (): SocialButtonValue => {
    const settings = getSocialLoginSettings();
    const redirectUri = `${settings.redirectDomain}/account/login`;
    return {
        position: settings.microsoft.indexOrder,
        element: (
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
        )
    } as SocialButtonValue;
};
