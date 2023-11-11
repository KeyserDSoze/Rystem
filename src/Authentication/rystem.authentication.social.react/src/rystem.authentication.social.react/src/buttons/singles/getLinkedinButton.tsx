import { ProviderType, SocialButtonValue, SocialLoginManager, getSocialLoginSettings } from "../..";
import { LoginSocialLinkedin } from 'reactjs-social-login';
import { LinkedInLoginButton } from 'react-social-login-buttons';

export const getLinkedinButton = (): SocialButtonValue => {
    const settings = getSocialLoginSettings();
    const redirectUri = `${settings.redirectDomain}/account/login`;
    return {
        position: settings.linkedin.indexOrder,
        element: (
            <div key="l">
                {settings.linkedin.clientId != null &&
                    <LoginSocialLinkedin
                        client_id={settings.linkedin.clientId}
                        redirect_uri={redirectUri}
                        scope={"profile email openid"}
                        onResolve={(x: any) => {
                            SocialLoginManager.Instance(null).updateToken(ProviderType.Linkedin, x.data?.code);
                        }}
                        onReject={() => {
                            settings.onLoginFailure({ code: 3, message: "error clicking social button.", provider: ProviderType.Linkedin });
                        }}
                        isOnlyGetToken={true}
                        isOnlyGetCode={true}
                    >
                        <LinkedInLoginButton />
                    </LoginSocialLinkedin>}
            </div>
        )
    } as SocialButtonValue;
};
