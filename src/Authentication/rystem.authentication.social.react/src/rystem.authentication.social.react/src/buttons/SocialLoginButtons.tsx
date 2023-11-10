import { getSocialLoginSettings, SocialLoginManager, SocialLoginSettings, ProviderType } from "..";
import {
    LoginSocialGoogle,
    LoginSocialMicrosoft,
    LoginSocialFacebook,
    LoginSocialGithub,
    LoginSocialAmazon
} from 'reactjs-social-login';

import {
    AmazonLoginButton,
    FacebookLoginButton,
    GithubLoginButton,
    GoogleLoginButton,
    MicrosoftLoginButton,
} from 'react-social-login-buttons';

interface SocialButtonValue {
    element: JSX.Element,
    position: number
}

const getGoogleButton = (settings: SocialLoginSettings, setProfile: (provider: ProviderType, code: any) => void): SocialButtonValue => {
    const redirectUri = `${settings.redirectDomain}/account/login`;
    return {
        position: settings.google.indexOrder,
        element: (
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
                            setProfile(ProviderType.Google, x.data?.code);
                        }}
                        onReject={() => {
                            settings.onLoginFailure({ code: 3, message: "error clicking social button.", provider: ProviderType.Google });
                        }}>
                        <GoogleLoginButton />
                    </LoginSocialGoogle>
                }
            </div>
        )
    } as SocialButtonValue;
}

const getMicrosoftButton = (settings: SocialLoginSettings, setProfile: (provider: number, code: any) => void): SocialButtonValue => {
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
                            setProfile(ProviderType.Microsoft, x.data?.id_token);
                        }}
                        onReject={() => {
                            settings.onLoginFailure({ code: 3, message: "error clicking social button.", provider: ProviderType.Microsoft });
                        }}
                        isOnlyGetToken={true}
                    >
                        <MicrosoftLoginButton />
                    </LoginSocialMicrosoft>
                }
            </div>
        )
    } as SocialButtonValue;
}

const getFacebookButton = (settings: SocialLoginSettings, setProfile: (provider: number, code: any) => void): SocialButtonValue => {
    const redirectUri = `${settings.redirectDomain}/account/login`;
    return {
        position: settings.facebook.indexOrder,
        element: (
            <div key="f">
                {settings.facebook.clientId != null &&
                    <LoginSocialFacebook
                        appId={settings.facebook.clientId}
                        fieldsProfile={
                            'id,first_name,last_name,middle_name,name,name_format,picture,short_name,email,gender'
                        }
                        redirect_uri={redirectUri}
                        onResolve={(x: any) => {
                            setProfile(ProviderType.Facebook, x.data?.accessToken);
                        }}
                        onReject={() => {
                            settings.onLoginFailure({ code: 3, message: "error clicking social button.", provider: ProviderType.Facebook });
                        }}
                        isOnlyGetToken={true}
                    >
                        <FacebookLoginButton />
                    </LoginSocialFacebook>
                }
            </div>
        )
    } as SocialButtonValue;
}

const getGitHub = (settings: SocialLoginSettings, setProfile: (provider: number, code: any) => void): SocialButtonValue => {
    const redirectUri = `${settings.redirectDomain}/account/login`;
    return {
        position: settings.github.indexOrder,
        element: (
            <div key="h">
                {settings.github.clientId != null &&
                    <LoginSocialGithub
                        client_id={settings.github.clientId}
                        client_secret={""}
                        scope="user:email"
                        redirect_uri={redirectUri}
                        onResolve={(x: any) => {
                            setProfile(ProviderType.GitHub, x.data?.code);
                        }}
                        onReject={() => {
                            settings.onLoginFailure({ code: 3, message: "error clicking social button.", provider: ProviderType.GitHub });
                        }}
                        isOnlyGetToken={true}
                        isOnlyGetCode={true}
                    >
                        <GithubLoginButton />
                    </LoginSocialGithub>
                }
            </div>
        )
    } as SocialButtonValue;
}

const getAmazon = (settings: SocialLoginSettings, setProfile: (provider: number, code: any) => void): SocialButtonValue => {
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
                            setProfile(ProviderType.Amazon, x.data?.access_token);
                        }}
                        onReject={() => {
                            settings.onLoginFailure({ code: 3, message: "error clicking social button.", provider: ProviderType.Amazon });
                        }}
                        isOnlyGetToken={true}
                        isOnlyGetCode={true}
                    >
                        <AmazonLoginButton />
                    </LoginSocialAmazon>
                }
            </div>
        )
    } as SocialButtonValue;
}


const getButtons = new Array<(settings: SocialLoginSettings, setProfile: (provider: number, code: any) => void) => SocialButtonValue>;
getButtons.push(getGoogleButton);
getButtons.push(getMicrosoftButton);
getButtons.push(getFacebookButton);
getButtons.push(getGitHub);
getButtons.push(getAmazon);

export const SocialLoginButtons = () => {
    const settings = getSocialLoginSettings();
    const setProfile = (provider: number, code: any) => {
        SocialLoginManager.Instance(null).updateToken(provider, code);
    };
    return (
        <>
            {settings.title != null && <h1 className="title">{settings.title}</h1>}
            {getButtons
                .sort((x1, x2) => x1(settings, setProfile).position - x2(settings, setProfile).position)
                .map(value => value(settings, setProfile).element)}
        </>
    );
}