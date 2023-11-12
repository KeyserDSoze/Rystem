import { SocialLoginErrorResponse, SocialParameter } from "../..";

export interface SocialLoginSettings {
    apiUri: string;
    redirectDomain: string;
    automaticRefresh: boolean;
    onLoginFailure: (data: SocialLoginErrorResponse) => void;
    title: string | null;
    google: SocialParameter;
    microsoft: SocialParameter;
    facebook: SocialParameter;
    github: SocialParameter;
    amazon: SocialParameter;
    linkedin: SocialParameter;
    x: SocialParameter;
    instagram: SocialParameter;
    pinterest: SocialParameter;
}