import { ModernSocialButton } from "../../components/ModernSocialButton";
import { GitHubIcon } from "../../components/BrandIcons";

export const GitHubLoginButton = () => {
    return (
        <ModernSocialButton
            provider="github"
            text="Continue with GitHub"
            icon={<GitHubIcon />}
        />
    );
};
