import { useContext } from "react";
import { SocialLoginContextLogout } from "../index";

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
