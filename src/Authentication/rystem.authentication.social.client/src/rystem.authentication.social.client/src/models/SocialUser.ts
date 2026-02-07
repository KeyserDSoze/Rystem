export interface ISocialUser {
    username: string;
    isAuthenticated: boolean;
}

export type SocialUser<T = {}> = ISocialUser & T;