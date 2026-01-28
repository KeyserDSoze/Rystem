import { CreateSocialButton, ProviderType, SocialButtonProps, getSocialLoginSettings } from "../..";
import { MicrosoftLoginButton } from "../graphics/MicrosoftLoginButton";
import { generateCodeChallenge, generateCodeVerifier, storeCodeVerifier } from "../../utils/pkce";
import { useEffect, useState } from "react";

export const MicrosoftButton = ({ className = '', }: SocialButtonProps): JSX.Element => {
    const settings = getSocialLoginSettings();
    const [oauthUrl, setOauthUrl] = useState<string | null>(null);
    
    useEffect(() => {
        if (settings.microsoft.clientId) {
            // Generate PKCE values
            const verifier = generateCodeVerifier();
            
            generateCodeChallenge(verifier).then(challenge => {
                // Store verifier for later use (after OAuth callback)
                storeCodeVerifier('microsoft', verifier);
                
                const redirectUri = `${settings.redirectDomain}${settings.redirectPath}`;
                
                // Build OAuth URL with code_challenge and S256 method
                const url = `https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?client_id=${settings.microsoft.clientId}
                    &response_type=code
                    &redirect_uri=${redirectUri}
                    &response_mode=query
                    &scope=profile%20openid%20email
                    &state=${ProviderType.Microsoft}
                    &prompt=select_account
                    &code_challenge=${challenge}
                    &code_challenge_method=S256`;
                
                setOauthUrl(url);
            });
        }
    }, [settings.microsoft.clientId]);
    
    if (settings.microsoft.clientId && oauthUrl) {
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
