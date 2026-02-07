import { ModernSocialButton } from "../../components/ModernSocialButton";
import { InstagramIcon } from "../../components/BrandIcons";

export const InstagramLoginButton = () => {
    return (
        <ModernSocialButton
            provider="instagram"
            text="Continue with Instagram"
            icon={<InstagramIcon />}
        />
    );
};
