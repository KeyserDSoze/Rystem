export interface SocialButtonStyle {
    activeBackground: string;
    background: string;
    iconColor: string;
    color: string;
    text: string;
    icon: (configuration: SocialButtonStyle) => React.JSX.Element;
}
