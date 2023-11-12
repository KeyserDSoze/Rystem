import { SocialButtonProps } from "..";

export interface SocialButtonsProps extends SocialButtonProps {
    buttons?: Array<(x: SocialButtonProps) => JSX.Element>;
}
