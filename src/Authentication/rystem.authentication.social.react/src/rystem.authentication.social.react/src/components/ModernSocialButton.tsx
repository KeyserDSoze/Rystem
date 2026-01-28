import React from 'react';
import '../styles/SocialButton.css';

export interface ModernSocialButtonProps {
    provider: 'microsoft' | 'google' | 'facebook' | 'github' | 'x' | 'linkedin' | 'amazon' | 'instagram' | 'tiktok' | 'pinterest';
    text: string;
    icon: React.ReactNode;
    onClick?: () => void;
    disabled?: boolean;
    loading?: boolean;
    className?: string;
    ariaLabel?: string;
}

/**
 * Modern Social Button Component
 * - Dark mode ready (CSS variables)
 * - Accessible (ARIA labels, keyboard navigation)
 * - Responsive (mobile-friendly)
 * - Brand guidelines compliant
 */
export const ModernSocialButton: React.FC<ModernSocialButtonProps> = ({
    provider,
    text,
    icon,
    onClick,
    disabled = false,
    loading = false,
    className = '',
    ariaLabel,
}) => {
    const buttonClasses = [
        'rystem-social-button',
        `rystem-social-button--${provider}`,
        loading && 'rystem-social-button--loading',
        className
    ].filter(Boolean).join(' ');

    return (
        <button
            type="button"
            className={buttonClasses}
            onClick={onClick}
            disabled={disabled || loading}
            aria-label={ariaLabel || `Sign in with ${provider.charAt(0).toUpperCase() + provider.slice(1)}`}
            aria-busy={loading}
        >
            <span className="rystem-social-button__icon" aria-hidden="true">
                {icon}
            </span>
            <span className="rystem-social-button__text">
                {text}
            </span>
        </button>
    );
};

export default ModernSocialButton;
