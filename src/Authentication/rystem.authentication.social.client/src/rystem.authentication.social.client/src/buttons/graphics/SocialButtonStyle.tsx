export interface SocialButtonStyle {
    activeBackground: string;
    background: string;
    iconColor: string;
    color: string;
    text: string;
    icon: (configuration: SocialButtonStyle) => JSX.Element;
}
