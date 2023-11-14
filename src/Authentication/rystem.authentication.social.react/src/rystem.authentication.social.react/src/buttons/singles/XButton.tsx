import { CreateSocialButton, ProviderType, SocialButtonProps, getSocialLoginSettings } from "../..";
import { TwitterLoginButton } from 'react-social-login-buttons';

export const XButton = ({ className = '', }: SocialButtonProps): JSX.Element => {
    const settings = getSocialLoginSettings();
    if (settings.x.clientId) {
        const redirectUri = `${settings.redirectDomain}/account/login`;
        const oauthUrl = `https://twitter.com/i/oauth2/authorize?response_type=code
            &client_id=${settings.x.clientId}
            &redirect_uri=${redirectUri}
            &scope=users.read%20tweet.read
            &state=${ProviderType.X}
            &code_challenge=challenge
            &code_challenge_method=plain`;
        return (
            <CreateSocialButton
                key="x"
                provider={ProviderType.X}
                redirect_uri={oauthUrl}
                className={className}
            >
                <TwitterLoginButton />
            </CreateSocialButton>
        );
    } else {
        return (<></>);
    }
};
