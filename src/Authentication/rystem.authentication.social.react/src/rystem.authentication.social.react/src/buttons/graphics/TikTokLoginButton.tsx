import { ModernSocialButton } from "../../components/ModernSocialButton";
import { TikTokIcon } from "../../components/BrandIcons";

export const TikTokLoginButton = () => {
    return (
        <ModernSocialButton
            provider="tiktok"
            text="Continue with TikTok"
            icon={<TikTokIcon />}
        />
    );
};



