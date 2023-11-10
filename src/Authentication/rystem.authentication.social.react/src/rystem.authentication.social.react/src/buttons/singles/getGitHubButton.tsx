import { ProviderType, SocialButtonValue, SocialLoginManager, getSocialLoginSettings } from "../..";
import { LoginSocialGithub } from 'reactjs-social-login';
import { GithubLoginButton } from 'react-social-login-buttons';

export const getGitHubButton = (): SocialButtonValue => {
    const settings = getSocialLoginSettings();
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
                            SocialLoginManager.Instance(null).updateToken(ProviderType.GitHub, x.data?.code);
                        }}
                        onReject={() => {
                            settings.onLoginFailure({ code: 3, message: "error clicking social button.", provider: ProviderType.GitHub });
                        }}
                        isOnlyGetToken={true}
                        isOnlyGetCode={true}
                    >
                        <GithubLoginButton />
                    </LoginSocialGithub>}
            </div>
        )
    } as SocialButtonValue;
};
