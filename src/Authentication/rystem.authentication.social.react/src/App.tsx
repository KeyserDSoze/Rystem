import './App.css'
import { Wrapper } from './Wrapper'
import { SocialLoginWrapper, setupSocialLogin, IIdentityTransformer, PlatformType, LoginMode } from './rystem.authentication.social.react/src';

export interface SuperSocialUser {
    email: string;
    username?: string;
    language?: string;
}

export const SuperSocialUserTransformer: IIdentityTransformer<SuperSocialUser> = {
    toPlain: (input: SuperSocialUser): any => {
        return {
            e: input.email,
            u: input.username ?? null,
            l: input.language ?? null,
        };
    },

    fromPlain: (input: any): SuperSocialUser => {
        return {
            email: input.e,
            username: input.u,
            language: input.l,
        };
    },

    retrieveUsername: (input: SuperSocialUser): string => {
        return input.username ?? input.email;
    }
};

setupSocialLogin(x => {
    x.apiUri = "https://localhost:7017";  // ✅ Corretto: apiUri (come definito in SocialLoginSettings.ts)
    x.identityTransformer = SuperSocialUserTransformer,
        x.platform = {
            type: PlatformType.Web,
        redirectPath: "/account/login",
        loginMode: LoginMode.Redirect
        };
    x.google.clientId = "23769141170-lfs24avv5qrj00m4cbmrm202c0fc6gcg.apps.googleusercontent.com";
    x.microsoft.clientId = "0b90db07-be9f-4b29-b673-9e8ee9265927";
    x.facebook.clientId = "345885718092912";
    x.github.clientId = "97154d062f2bb5d28620"
    x.amazon.clientId = "amzn1.application-oa2-client.dffbc466d62c44e49d71ad32f4aecb62"
    x.linkedin.clientId = "77cegp267wifhs";
    x.x.clientId = "a1VMWE84S0I2ZFZRMmxzSmN3bVE6MTpjaQ";
    x.instagram.clientId = "2316623861862040";
    x.pinterest.clientId = "1492670";
    x.tiktok.clientId = "aw2jon58in6azihv";
    x.onLoginFailure = (data) => alert(data.message);
    x.automaticRefresh = true;
});
function App() {
    return (
        <>
            <SocialLoginWrapper>
                <Wrapper></Wrapper>
            </SocialLoginWrapper>
        </>
    )
}

export default App
