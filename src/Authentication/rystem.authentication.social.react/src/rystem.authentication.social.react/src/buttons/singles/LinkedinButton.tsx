import { ProviderType, SocialButtonProps, getSocialLoginSettings, CreateSocialButton } from "../..";
import { LinkedInLoginButton } from "../graphics/LinkedInLoginButton";

export const LinkedinButton = ({ className = '', }: SocialButtonProps): JSX.Element => {
    const settings = getSocialLoginSettings();
    if (settings.linkedin.clientId) {
        const redirectUri = `${settings.redirectDomain}/account/login`;
        const scope = "profile email openid";
        const uri = `https://www.linkedin.com/oauth/v2/authorization?response_type=code
                    &client_id=${settings.linkedin.clientId}
                    &scope=${scope}
                    &state=${ProviderType.Linkedin}
                    &redirect_uri=${redirectUri}`;
        return (
            <div key="l">
                <CreateSocialButton
                    provider={ProviderType.Linkedin}
                    redirect_uri={uri}
                    className={className}
                >
                    <LinkedInLoginButton />
                </CreateSocialButton>
            </div>
        );
    }
    else {
        return (<></>);
    }
};
