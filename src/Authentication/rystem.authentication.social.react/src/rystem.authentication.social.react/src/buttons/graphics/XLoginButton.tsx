import { ModernSocialButton } from "../../components/ModernSocialButton";
import { XIcon } from "../../components/BrandIcons";

export const XLoginButton = () => {
    return (
        <ModernSocialButton
            provider="x"
            text="Continue with X"
            icon={<XIcon />}
        />
    );
};




