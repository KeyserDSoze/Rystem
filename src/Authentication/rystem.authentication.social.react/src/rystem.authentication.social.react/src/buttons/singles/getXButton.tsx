import { ProviderType, SocialButtonValue, SocialLoginManager, getSocialLoginSettings } from "../..";
import { LoginSocialTwitter } from 'reactjs-social-login';
import { TwitterLoginButton } from 'react-social-login-buttons';

export const getXButton = (): SocialButtonValue => {
    const settings = getSocialLoginSettings();
    const redirectUri = `${settings.redirectDomain}/account/login`;
    return {
        position: settings.x.indexOrder,
        element: (
            <div key="x">
                {settings.x.clientId != null &&
                    <LoginSocialTwitter
                        client_id={settings.x.clientId}
                        scope="users.read tweet.read"
                        redirect_uri={redirectUri}
                        onResolve={(x: any) => {
                            SocialLoginManager.Instance(null).updateToken(ProviderType.X, x.data?.code);
                        }}
                        onReject={() => {
                            settings.onLoginFailure({ code: 3, message: "error clicking social button.", provider: ProviderType.X });
                        }}
                        isOnlyGetToken={true}
                        isOnlyGetCode={true}
                    >
                        <TwitterLoginButton />
                    </LoginSocialTwitter>}
            </div>
        )
    } as SocialButtonValue;
};
