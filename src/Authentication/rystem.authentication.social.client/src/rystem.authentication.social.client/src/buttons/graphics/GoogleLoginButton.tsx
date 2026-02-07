import { ModernSocialButton } from "../../components/ModernSocialButton";
import { GoogleIcon } from "../../components/BrandIcons";

export const GoogleLoginButton = () => {
    return (
        <ModernSocialButton
            provider="google"
            text="Continue with Google"
            icon={<GoogleIcon />}
        />
    );
};

