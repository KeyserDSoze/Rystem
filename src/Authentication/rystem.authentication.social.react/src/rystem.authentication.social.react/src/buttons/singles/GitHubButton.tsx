import { CreateSocialButton, ProviderType, SocialButtonProps, getSocialLoginSettings, buildRedirectUri } from "../..";
import { GitHubLoginButton } from "../graphics/GithubLoginButton";

export const GitHubButton = ({ className = '', }: SocialButtonProps): JSX.Element => {
const settings = getSocialLoginSettings();
const redirectUri = buildRedirectUri(settings);
if (settings.github.clientId) {
    const scope = "user:email";
    const oauthUrl = `https://github.com/login/oauth/authorize?client_id=${settings.github.clientId}&scope=${scope}&state=${ProviderType.GitHub}&redirect_uri=${encodeURIComponent(redirectUri)}&allow_signup=true`;
        return (
            <div key="h">
                <CreateSocialButton
                    provider={ProviderType.GitHub}
                    redirect_uri={oauthUrl}
                    className={className}
                >
                    <GitHubLoginButton />
                </CreateSocialButton>
            </div>
        );
    }
    else {
        return (<></>);
    }
};
