import { ProviderType, SocialButtonValue, SocialLoginManager, getSocialLoginSettings } from "../..";
import { LoginSocialFacebook } from 'reactjs-social-login';
import { FacebookLoginButton } from 'react-social-login-buttons';

export const getFacebookButton = (): SocialButtonValue => {
    const settings = getSocialLoginSettings();
    const redirectUri = `${settings.redirectDomain}/account/login`;
    return {
        position: settings.facebook.indexOrder,
        element: (
            <div key="f">
                {settings.facebook.clientId != null &&
                    <LoginSocialFacebook
                        appId={settings.facebook.clientId}
                        fieldsProfile={'id,first_name,last_name,middle_name,name,name_format,picture,short_name,email,gender'}
                        redirect_uri={redirectUri}
                        onResolve={(x: any) => {
                            SocialLoginManager.Instance(null).updateToken(ProviderType.Facebook, x.data?.accessToken);
                        }}
                        onReject={() => {
                            settings.onLoginFailure({ code: 3, message: "error clicking social button.", provider: ProviderType.Facebook });
                        }}
                        isOnlyGetToken={true}
                    >
                        <FacebookLoginButton />
                    </LoginSocialFacebook>}
            </div>
        )
    } as SocialButtonValue;
};
