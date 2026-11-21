import { SocialButtonProps } from "..";

export interface SocialButtonsProps extends SocialButtonProps {
    buttons?: Array<(x: SocialButtonProps) => React.JSX.Element>;
}
