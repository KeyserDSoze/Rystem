### [What is Rystem?](https://github.com/KeyserDSoze/Rystem)

# Social logins

## Services setup
You need to install the configuration in the startup file.


```
import { SocialLoginWrapper, setupSocialLogin } from 'rystem.authentication.social.react';

setupSocialLogin(x => {
    x.apiUri = "https://localhost:7017";
    x.google.clientId = "23769141170-lfs24avv5qrj00m4cbmrm202c0fc6gcg.apps.googleusercontent.com";
    x.google.indexOrder = 1;
    x.microsoft.clientId = "0b90db07-be9f-4b29-b673-9e8ee9265927";
    x.microsoft.indexOrder = 0;
    x.facebook.clientId = "345885718092912";
    x.facebook.indexOrder = 2;
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
For example the Wrapper component could be so

```
import { useContext, useState } from "react";
import reactLogo from './assets/react.svg'
import { SocialLoginButtons, SocialLoginContextLogout, SocialLoginContextRefresh, SocialLogoutButton, useSocialToken, useSocialUser } from "rystem.authentication.social.react";

export const Wrapper = () => {
    const token = useSocialToken();
    const [count, setCount] = useState(0);
    const user = useSocialUser();
    const forceRefresh = useContext(SocialLoginContextRefresh);
    const logout = useContext(SocialLoginContextLogout);
    return (
        <>
            <div>
                <a href="https://react.dev" target="_blank">
                    <img src={reactLogo} className="logo react" alt="React logo" />
                </a>
            </div>
            <h1>Vite + React</h1>
            <div className="card">
                <button onClick={() => setCount((count) => count + 1)}>
                    count is {count}
                </button>
                <p>
                    Edit <code>src/App.tsx</code> and save to test HMR
                </p>
            </div>
            <p className="read-the-docs">
                Click on the Vite and React logos to learn more
            </p>
            {token.isExpired && <SocialLoginButtons></SocialLoginButtons>}
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
