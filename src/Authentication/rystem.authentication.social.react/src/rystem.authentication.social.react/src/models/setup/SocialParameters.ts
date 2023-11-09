import { SocialParameter } from "./SocialParameter";

export interface SocialParameterWithSecret extends SocialParameter {
    secretId: string | null;
}
