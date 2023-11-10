import { SocialParameter } from "../..";

export interface SocialParameterWithSecret extends SocialParameter {
    secretId: string | null;
}
