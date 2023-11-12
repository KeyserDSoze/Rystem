import { ProviderType, SocialLoginManager, getSocialLoginSettings } from "../..";
import { LoginSocialPinterest } from 'reactjs-social-login';
import { createButton } from 'react-social-login-buttons';

const config = {
    text: "Log in with Pinterest",
    icon: "pinterest",
    iconFormat: (name: string) => `fa fa-${name}`,
    style: { background: "#E60023" },
    activeStyle: { background: "#fff" }
};
const PinterestLoginButton = createButton(config);

export const PinterestButton = (): JSX.Element => {
    const settings = getSocialLoginSettings();
    const redirectUri = `${settings.redirectDomain}/account/login`;
    return (
        <div key="p">
            {settings.pinterest.clientId != null &&
                <LoginSocialPinterest
                    client_id={settings.pinterest.clientId}
                    scope="users.read tweet.read"
                    redirect_uri={redirectUri}
                    onResolve={(x: any) => {
                        SocialLoginManager.Instance(null).updateToken(ProviderType.Pinterest, x.data?.code);
                    }}
                    onReject={() => {
                        settings.onLoginFailure({ code: 3, message: "error clicking social button.", provider: ProviderType.Pinterest });
                    }}
                    isOnlyGetToken={true}
                    isOnlyGetCode={true}
                >
                    <PinterestLoginButton />
                </LoginSocialPinterest>}
        </div>
    );
};
