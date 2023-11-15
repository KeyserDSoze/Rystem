import { CreateSocialButton, ProviderType, SocialButtonProps, getSocialLoginSettings } from "../..";
import { createButton } from 'react-social-login-buttons';

const config = {
    text: "Log in with Pinterest",
    icon: "pinterest",
    iconFormat: (name: string) => `fa fa-${name}`,
    style: { background: "#E60023" },
    activeStyle: { background: "#F60023" }
};
const PinterestLoginButton = createButton(config);

export const PinterestButton = ({ className = '', }: SocialButtonProps): JSX.Element => {
    const settings = getSocialLoginSettings();
    if (settings.pinterest.clientId) {
        const redirectUri = `${settings.redirectDomain}/account/login`;
        const oauthUrl = `https://www.pinterest.com/oauth/?client_id=${settings.pinterest.clientId}
            &scope=boards:read,pins:read,user_accounts:read
            &state=${ProviderType.Pinterest}
            &redirect_uri=${redirectUri}
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
