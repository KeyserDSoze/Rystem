import { CreateSocialButton, ProviderType, SocialButtonProps, getSocialLoginSettings } from "../..";
import { MicrosoftLoginButton } from "../graphics/MicrosoftLoginButton";

export const MicrosoftButton = ({ className = '', }: SocialButtonProps): JSX.Element => {
    const settings = getSocialLoginSettings();
    if (settings.microsoft.clientId) {
        const redirectUri = `${settings.redirectDomain}${settings.redirectPath}`;
        const oauthUrl = `https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?client_id=${settings.microsoft.clientId}
                &response_type=code
                &redirect_uri=${redirectUri}
                &response_mode=query
                &scope=profile%20openid%20email
                &state=${ProviderType.Microsoft}
                &prompt=select_account
                &code_challenge=19cfc47c216dacba8ca23eeee817603e2ba34fe0976378662ba31688ed302fa9
                &code_challenge_method=plain`;
        return (
            <CreateSocialButton
                key="m"
                provider={ProviderType.Microsoft}
                redirect_uri={oauthUrl}
                className={className}
            >
                <MicrosoftLoginButton />
            </CreateSocialButton>
        );
    } else {
        return (<></>);
    }
};
