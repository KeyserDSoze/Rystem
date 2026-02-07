import { ProviderType } from "../..";

export interface SocialLoginErrorResponse {
    code: number;
    message: string;
    provider: ProviderType;
}

