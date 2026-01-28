import { ModernSocialButton } from "../../components/ModernSocialButton";
import { FacebookIcon } from "../../components/BrandIcons";

export const FacebookLoginButton = () => {
    return (
        <ModernSocialButton
            provider="facebook"
            text="Continue with Facebook"
            icon={<FacebookIcon />}
        />
    );
};
