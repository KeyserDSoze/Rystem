import { ModernSocialButton } from "../../components/ModernSocialButton";
import { PinterestIcon } from "../../components/BrandIcons";

export const PinterestLoginButton = () => {
    return (
        <ModernSocialButton
            provider="pinterest"
            text="Continue with Pinterest"
            icon={<PinterestIcon />}
        />
    );
};

