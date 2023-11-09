import { useReducer } from "react";
import { useSocialToken } from "../hooks/useSocialToken";
import { SocialLoginContextLogout, SocialLoginContextRefresh, SocialLoginContextUpdate, removeSocialLogin } from "../index";
import { SocialLoginManager } from "../setup/SocialLoginManager";

const forceRefresh = () => {
    const oldToken = useSocialToken();
    SocialLoginManager.Instance(null).updateToken(0, oldToken.refreshToken);
};

export const SocialLoginWrapper = (c: { children: any; }) => {
    const [renderingKey, forceUpdate] = useReducer(x => x + 1, 0);
    SocialLoginManager.Instance(null).refresher = () => forceUpdate();
    const forceLogout = () => {
        removeSocialLogin();
        forceUpdate();
    };
    return (
        <div key={renderingKey}>
            <SocialLoginContextUpdate.Provider value={forceUpdate}>
                <SocialLoginContextRefresh.Provider value={forceRefresh}>
                    <SocialLoginContextLogout.Provider value={forceLogout}>
                        {c.children}
                    </SocialLoginContextLogout.Provider>
                </SocialLoginContextRefresh.Provider>
            </SocialLoginContextUpdate.Provider>
        </div>
    );
};
