import { useContext, useState } from "react";
import reactLogo from './assets/react.svg'
import { SocialLoginButtons, SocialLoginContextLogout, SocialLoginContextRefresh, SocialLogoutButton, useSocialToken, useSocialUser } from "./rystem.authentication.social.react/src/index";

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