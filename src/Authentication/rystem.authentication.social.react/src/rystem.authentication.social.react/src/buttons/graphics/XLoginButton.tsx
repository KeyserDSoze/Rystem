import { SocialButtonStyle } from "./SocialButtonStyle";
import { SocialLoginButton } from "./SocialLoginButton";

const configuration = {
    activeBackground: "#333333",
    background: '#000000',
    iconColor: "#fff",
    color: "#fff",
    text: "Log in with X",
    icon: () => (<svg xmlns="http://www.w3.org/2000/svg" fill={configuration.iconColor} viewBox="0 0 50 50" style={{ width: "100%", height: "auto" }}><path d="M 6.9199219 6 L 21.136719 26.726562 L 6.2285156 44 L 9.40625 44 L 22.544922 28.777344 L 32.986328 44 L 43 44 L 28.123047 22.3125 L 42.203125 6 L 39.027344 6 L 26.716797 20.261719 L 16.933594 6 L 6.9199219 6 z" /></svg>)
} as SocialButtonStyle;

export const XLoginButton = () => {
    return (<SocialLoginButton {...configuration}>
    </SocialLoginButton>)
}




