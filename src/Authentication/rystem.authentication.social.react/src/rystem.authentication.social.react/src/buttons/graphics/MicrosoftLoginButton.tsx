import { ModernSocialButton } from "../../components/ModernSocialButton";
import { MicrosoftIcon } from "../../components/BrandIcons";

export const MicrosoftLoginButton = () => {
    return (
        <ModernSocialButton
            provider="microsoft"
            text="Continue with Microsoft"
            icon={<MicrosoftIcon />}
        />
    );
};


