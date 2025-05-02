import { CreateSocialButton, ProviderType, SocialButtonProps, getSocialLoginSettings } from "../..";
import { TikTokLoginButton } from "../graphics/TikTokLoginButton";

export const TikTokButton = ({ className = '', }: SocialButtonProps): JSX.Element => {
    const settings = getSocialLoginSettings();
    if (settings.tiktok.clientId) {
        const redirectUri = `${settings.redirectDomain}${settings.redirectPath}`;
        const oauthUrl = `https://www.tiktok.com/v2/auth/authorize/?client_key=${settings.tiktok.clientId}&scope=user.info.basic&state=${ProviderType.TikTok}&redirect_uri=${encodeURIComponent(redirectUri)}&response_type=code`;
        return (
            <CreateSocialButton
                key="tk"
                provider={ProviderType.TikTok}
                redirect_uri={oauthUrl}
                className={className}
            >
                <TikTokLoginButton />
            </CreateSocialButton>
        );
    } else {
        return (<></>);
    }
};
