import { ProviderType, SocialButtonValue, SocialLoginManager, getSocialLoginSettings } from "../..";
import { LoginSocialAmazon } from 'reactjs-social-login';
import { AmazonLoginButton } from 'react-social-login-buttons';

export const getAmazonButton = (): SocialButtonValue => {
    const settings = getSocialLoginSettings();
    const redirectUri = `${settings.redirectDomain}/account/login`;
    return {
        position: settings.amazon.indexOrder,
        element: (
            <div key="a">
                {settings.amazon.clientId != null &&
                    <LoginSocialAmazon
                        client_id={settings.amazon.clientId}
                        client_secret={""}
                        scope="profile"
                        redirect_uri={redirectUri}
                        onResolve={(x: any) => {
                            SocialLoginManager.Instance(null).updateToken(ProviderType.Amazon, x.data?.access_token);
                        }}
                        onReject={() => {
                            settings.onLoginFailure({ code: 3, message: "error clicking social button.", provider: ProviderType.Amazon });
                        }}
                        isOnlyGetToken={true}
                        isOnlyGetCode={true}
                    >
                        <AmazonLoginButton />
                    </LoginSocialAmazon>}
            </div>
        )
    } as SocialButtonValue;
};
