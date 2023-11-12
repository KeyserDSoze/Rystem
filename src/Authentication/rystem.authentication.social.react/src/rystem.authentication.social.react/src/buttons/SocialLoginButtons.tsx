import { getSocialLoginSettings, AmazonButton, GoogleButton, MicrosoftButton, FacebookButton, GitHubButton, LinkedinButton, XButton, PinterestButton, InstagramButton, SocialButtonsProps } from "..";

const defaulButtons = [
    GoogleButton,
    MicrosoftButton,
    FacebookButton,
    LinkedinButton,
    GitHubButton,
    AmazonButton,
    XButton,
    InstagramButton,
    PinterestButton];

export const SocialLoginButtons = ({ className = '', buttons }: SocialButtonsProps) => {
    const settings = getSocialLoginSettings();
    return (
        <>
            {settings.title != null && <h1 className="title">{settings.title}</h1>}
            {(buttons ?? defaulButtons).map(value => value({ className }))}
        </>
    );
}