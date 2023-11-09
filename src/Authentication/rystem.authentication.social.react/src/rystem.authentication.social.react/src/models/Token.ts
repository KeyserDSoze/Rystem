export interface Token {
    accessToken: string;
    expiresIn: Date;
    refreshToken: string;
    isExpired: boolean;
}
