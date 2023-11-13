import './App.css'
import { Wrapper } from './Wrapper'
import { SocialLoginWrapper, setupSocialLogin } from './rystem.authentication.social.react/src';

setupSocialLogin(x => {
    x.apiUri = "https://localhost:7017";
    x.google.clientId = "23769141170-lfs24avv5qrj00m4cbmrm202c0fc6gcg.apps.googleusercontent.com";
    x.microsoft.clientId = "0b90db07-be9f-4b29-b673-9e8ee9265927";
    x.facebook.clientId = "345885718092912";
    x.github.clientId = "97154d062f2bb5d28620"
    x.amazon.clientId = "amzn1.application-oa2-client.dffbc466d62c44e49d71ad32f4aecb62"
    x.linkedin.clientId = "77cegp267wifhs";
    x.x.clientId = "a1VMWE84S0I2ZFZRMmxzSmN3bVE6MTpjaQ";
    x.instagram.clientId = "2316623861862040";
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
