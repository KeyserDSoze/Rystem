import { CreateSocialButton, ProviderType, SocialButtonProps, getSocialLoginSettings } from "../..";
import { InstagramLoginButton } from 'react-social-login-buttons';

export const InstagramButton = ({ className = '', }: SocialButtonProps): JSX.Element => {
    const settings = getSocialLoginSettings();
    if (settings.instagram.clientId) {
        const redirectUri = `${settings.redirectDomain}/account/login`;
        const oauthUrl = `https://api.instagram.com/oauth/authorize?response_type=code&client_id=${settings.instagram.clientId}&scope=user_profile,user_media&state=${ProviderType.Instagram}&redirect_uri=${redirectUri}`;
        return (
            <CreateSocialButton
                key="i"
                provider={ProviderType.Instagram}
                redirect_uri={oauthUrl}
                className={className}
            >
                <InstagramLoginButton />
            </CreateSocialButton>
        );
    } else {
        return (<></>);
    }
};
