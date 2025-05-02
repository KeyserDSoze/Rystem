import { SocialLoginErrorResponse, SocialParameter } from "../..";

export interface SocialLoginSettings {
    apiUri: string;
    redirectDomain: string;
    redirectPath: string;
    automaticRefresh: boolean;
    identityTransformer?: IIdentityTransformer<any>;
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
    tiktok: SocialParameter;
}

export interface IIdentityTransformer<TIdentity> {
    toPlain: (input: TIdentity | any) => any;
    fromPlain: (input: any) => TIdentity;
    retrieveUsername: (input: TIdentity) => string;
}