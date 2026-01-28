import { CreateSocialButton, ProviderType, SocialButtonProps, getSocialLoginSettings, buildRedirectUri } from "../..";
import { MicrosoftLoginButton } from "../graphics/MicrosoftLoginButton";
import { generateCodeChallenge, generateCodeVerifier } from "../../utils/pkce";
import { PkceStorageService } from "../../services/PkceStorageService";
import { useEffect, useState } from "react";

export const MicrosoftButton = ({ className = '', }: SocialButtonProps): JSX.Element => {
    const settings = getSocialLoginSettings();
    const [oauthUrl, setOauthUrl] = useState<string | null>(null);
    const pkceStorage = new PkceStorageService(settings.storageService);

    useEffect(() => {
        if (settings.microsoft.clientId) {
            // Check if PKCE verifier already exists (means we're returning from redirect)
            const existingVerifier = pkceStorage.getCodeVerifier('microsoft');
            
            if (existingVerifier) {
                // Redirect flow: Reuse existing PKCE (don't regenerate!)
                // This happens when OAuth provider redirects back to our app
                console.log('MicrosoftButton: Reusing existing PKCE verifier (redirect flow)');
                
                // Rebuild OAuth URL with existing challenge
                generateCodeChallenge(existingVerifier).then(challenge => {
                    const redirectUri = buildRedirectUri(settings);
                    const url = `https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?client_id=${settings.microsoft.clientId}
                        &response_type=code
                        &redirect_uri=${encodeURIComponent(redirectUri)}
                        &response_mode=query
                        &scope=profile%20openid%20email
                        &state=${ProviderType.Microsoft}
                        &prompt=select_account
                        &code_challenge=${challenge}
                        &code_challenge_method=S256`;
                    setOauthUrl(url);
                });
            } else {
                // Fresh flow: Generate new PKCE values
                const verifier = generateCodeVerifier();

                generateCodeChallenge(verifier).then(challenge => {
                    // Store verifier for later use (after OAuth callback)
                    pkceStorage.storeCodeVerifier('microsoft', verifier);
                    pkceStorage.storeCodeChallenge('microsoft', challenge);  // Optional: for debugging

                    // Build redirect URI using helper function
                    const redirectUri = buildRedirectUri(settings);
                    
                    // Build OAuth URL with code_challenge and S256 method
                    const url = `https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?client_id=${settings.microsoft.clientId}
                        &response_type=code
                        &redirect_uri=${encodeURIComponent(redirectUri)}
                        &response_mode=query
                        &scope=profile%20openid%20email
                        &state=${ProviderType.Microsoft}
                        &prompt=select_account
                        &code_challenge=${challenge}
                        &code_challenge_method=S256`;

                    setOauthUrl(url);
                });
            }
        }
    }, [settings.microsoft.clientId, settings.platform]);

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
