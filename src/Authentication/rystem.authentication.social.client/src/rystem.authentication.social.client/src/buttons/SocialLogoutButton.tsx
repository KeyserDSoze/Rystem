import { useContext } from "react";
import { SocialLoginContextLogout } from "..";

export const SocialLogoutButton = (c: { children: any; }) => {
    const logout = useContext(SocialLoginContextLogout);
    return (
        <>
            <button onClick={() => logout()}>
                {c.children}
            </button>
        </>
    );
};
