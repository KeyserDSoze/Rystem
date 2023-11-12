import { ProviderType, SocialLoginManager, getSocialLoginSettings } from "../..";
import { LoginSocialInstagram } from 'reactjs-social-login';
import { InstagramLoginButton } from 'react-social-login-buttons';

export const InstagramButton = (): JSX.Element => {
    const settings = getSocialLoginSettings();
    const redirectUri = `${settings.redirectDomain}/account/login`;
    return (
        <div key="x">
            {settings.instagram.clientId != null &&
                <LoginSocialInstagram
                    client_id={settings.instagram.clientId}
                    scope="user_profile,user_media"
                    redirect_uri={redirectUri}
                    onResolve={(x: any) => {
                        SocialLoginManager.Instance(null).updateToken(ProviderType.Instagram, x.data?.code);
                    }}
                    onReject={() => {
                        settings.onLoginFailure({ code: 3, message: "error clicking social button.", provider: ProviderType.Instagram });
                    }}
                    isOnlyGetToken={true}
                    isOnlyGetCode={true}
                >
                    <InstagramLoginButton />
                </LoginSocialInstagram>}
        </div>
    );
};
