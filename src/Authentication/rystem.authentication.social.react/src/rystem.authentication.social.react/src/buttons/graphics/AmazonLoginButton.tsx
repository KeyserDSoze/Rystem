import { ModernSocialButton } from '../../components/ModernSocialButton';
import { AmazonIcon } from '../../components/BrandIcons';

export const AmazonLoginButton = () => {
    return (
        <ModernSocialButton
            provider="amazon"
            text="Continue with Amazon"
            icon={<AmazonIcon />}
        />
    );
};

