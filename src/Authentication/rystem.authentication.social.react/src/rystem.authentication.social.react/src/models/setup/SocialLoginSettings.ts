import { SocialParameter } from "./SocialParameter";

export interface SocialLoginSettings {
    apiUri: string;
    redirectDomain: string;
    automaticRefresh: boolean;
    title: string | null;
    google: SocialParameter;
    microsoft: SocialParameter;
    facebook: SocialParameter;
}
