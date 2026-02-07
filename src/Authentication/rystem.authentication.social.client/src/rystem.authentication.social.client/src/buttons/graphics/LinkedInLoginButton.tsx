import { ModernSocialButton } from "../../components/ModernSocialButton";
import { LinkedInIcon } from "../../components/BrandIcons";

export const LinkedInLoginButton = () => {
    return (
        <ModernSocialButton
            provider="linkedin"
            text="Continue with LinkedIn"
            icon={<LinkedInIcon />}
        />
    );
};

