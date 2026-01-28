import { CreateSocialButton, ProviderType, SocialButtonProps, getSocialLoginSettings, buildRedirectUri } from "../..";
import { PinterestLoginButton } from "../graphics/PinterestLoginButton";

export const PinterestButton = ({ className = '', }: SocialButtonProps): JSX.Element => {
const settings = getSocialLoginSettings();
    if (settings.pinterest.clientId) {
        const redirectUri = buildRedirectUri(settings);
    const oauthUrl = `https://www.pinterest.com/oauth/?client_id=${settings.pinterest.clientId}
        &scope=boards:read,pins:read,user_accounts:read
        &state=${ProviderType.Pinterest}
        &redirect_uri=${encodeURIComponent(redirectUri)}
        &response_type=code`;
        return (
            <CreateSocialButton
                key="pi"
                provider={ProviderType.Pinterest}
                redirect_uri={oauthUrl}
                className={className}
            >
                <PinterestLoginButton />
            </CreateSocialButton>
        );
    } else {
        return (<></>);
    }
};
