import { IStorageService } from "./IStorageService";
import { SocialUser } from "../models/SocialUser";

/**
 * User storage service (decorator pattern)
 * Adds domain logic (JSON serialization, identity transformation) on top of generic storage
 */
export class UserStorageService {
private readonly userKey = 'socialUser';  // Backward compatible key

constructor(private storage: IStorageService) {}

    /**
     * Save social user information
     * @param user Social user data
     */
    saveUser<T = {}>(user: SocialUser<T>): void {
        try {
            this.storage.set(this.userKey, JSON.stringify(user));
        } catch (error) {
            console.error('UserStorageService: Error saving user', error);
        }
    }

    /**
     * Get social user information
     * Returns null if not found
     */
    getUser<T = {}>(): SocialUser<T> | null {
        try {
            const raw = this.storage.get(this.userKey);
            if (!raw) {
                return null;
            }

            return JSON.parse(raw) as SocialUser<T>;
        } catch (error) {
            console.error('UserStorageService: Error getting user', error);
            return null;
        }
    }

    /**
     * Check if user exists
     */
    hasUser(): boolean {
        return this.storage.has(this.userKey);
    }

    /**
     * Clear user from storage
     */
    clearUser(): void {
        this.storage.remove(this.userKey);
    }
}
