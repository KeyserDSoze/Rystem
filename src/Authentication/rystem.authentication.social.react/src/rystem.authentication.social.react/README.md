### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

# Social logins

## Services setup
You need to install the configuration in the startup file.


```
import { SocialLoginWrapper, setupSocialLogin } from 'rystem.authentication.social.react';

setupSocialLogin(x => {
    x.apiUri = "https://localhost:7017";
    x.google.clientId = "23769141170-lfs24avv5qrj00m4cbmrm202c0fc6gcg.apps.googleusercontent.com";
    x.microsoft.clientId = "0b90db07-be9f-4b29-b673-9e8ee9265927";
    x.facebook.clientId = "345885718092912";
    x.github.clientId = "97154d062f2bb5d28620"
    x.amazon.clientId = "amzn1.application-oa2-client.dffbc466d62c44e49d71ad32f4aecb62"
    x.onLoginFailure = (data) => alert(data.message); //set the callback for error during request.
    x.automaticRefresh = true; //this one help to autorefresh the token if expired during getting it.
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
```

### onLoginFailure callback
On error you can receive:
- code: 3, for error during clicking the social login button
- code: 15, for error during retrieve of the token
- code: 10, for error during retrieve of the user

Furthermore, you will receive a message and the social provider. When you see DotNet or 0, it means it's the integration with .Net api.

### Usage for token
Get current token to use for example in http request to your api services.
Check before the request if the token is expired.

```
const token = useSocialToken();
const isExpired = token.isExpired;
```

### Usage for user
Get current user to retrieve information about him.

```
const user = useSocialUser();
```

### Force refresh
Get the refresh action to use as you wish to refresh the token with a brand new one.
```
const forceRefresh = useContext(SocialLoginContextRefresh);
```

### Force logout
Get the logout action to set, for instance, as an event of a clicked button.
```
const logout = useContext(SocialLoginContextLogout);
```


### Example
For example the Wrapper component could be so, in this example you may find the way to change the login buttons order too.

```
import { SocialLoginButtons, SocialLoginContextLogout, SocialLoginContextRefresh, SocialLogoutButton, useSocialToken, useSocialUser } from "rystem.authentication.social.react";
import { AmazonButton, GoogleButton, MicrosoftButton, FacebookButton, GitHubButton, LinkedinButton, XButton, TikTokButton, PinterestButton, InstagramButton } from "rystem.authentication.social.react";

const newOrderButtons = [
    MicrosoftButton,
    GoogleButton,
    LinkedinButton,
    FacebookButton,
    AmazonButton,
    GitHubButton,
    XButton,
    TikTokButton,
    InstagramButton,
    PinterestButton
];

export const Wrapper = () => {
    const token = useSocialToken();
    const [count, setCount] = useState(0);
    const user = useSocialUser();
    const forceRefresh = useContext(SocialLoginContextRefresh);
    const logout = useContext(SocialLoginContextLogout);
    return (
        <>
            {token.isExpired && <SocialLoginButtons buttons={newOrderButtons}></SocialLoginButtons>}
            {!token.isExpired && <div>{token.accessToken}</div>}
            {user.isAuthenticated && <div>{user.username}</div>}
            <button onClick={() => forceRefresh()}>force refresh</button>
            <button onClick={() => logout()}>logout</button>
            <SocialLogoutButton>
                logout
            </SocialLogoutButton>
        </>
    );
}
```
