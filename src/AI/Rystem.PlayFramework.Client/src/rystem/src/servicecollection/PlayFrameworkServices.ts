import { PlayFrameworkClient } from "../engine/PlayFrameworkClient";
import { PlayFrameworkSettings } from "./PlayFrameworkSettings";

/**
 * Service collection for PlayFramework clients (Dependency Injection pattern).
 */
export class PlayFrameworkServices {
    private static clients: Map<string, PlayFrameworkClient> = new Map();
    private static settings: Map<string, PlayFrameworkSettings> = new Map();

    /**
     * Configure a PlayFramework client factory.
     * Always returns a Promise for consistency (even if configure is sync).
     * 
     * @param name - Factory name (unique identifier).
     * @param baseUrl - Base API URL (e.g., "https://api.example.com/api/ai").
     * @param configure - Optional configuration callback (sync or async).
     * @returns Promise resolving to PlayFrameworkSettings instance.
     */
    public static async configure(
        name: string,
        baseUrl: string,
        configure?: (settings: PlayFrameworkSettings) => void | Promise<void>
    ): Promise<PlayFrameworkSettings> {
        const settings = new PlayFrameworkSettings(name, baseUrl);

        try {
            if (configure) {
                await configure(settings);
            }

            this.settings.set(name, settings);
            this.clients.set(name, new PlayFrameworkClient(settings));
            return settings;
        } catch (error) {
            // Clean up partial state on failure
            this.settings.delete(name);
            this.clients.delete(name);
            throw new Error(
                `PlayFramework configuration failed for '${name}': ${error instanceof Error ? error.message : String(error)}`
            );
        }
    }

    /**
     * Get PlayFramework client by factory name.
     * 
     * @param name - Factory name.
     * @returns PlayFrameworkClient instance.
     * @throws Error if factory not configured.
     */
    public static getClient(name: string): PlayFrameworkClient {
        const client = this.clients.get(name);

        if (!client) {
            throw new Error(`PlayFramework client '${name}' not configured. Call PlayFrameworkServices.configure(name, baseUrl) first.`);
        }

        return client;
    }

    /**
     * Get PlayFramework settings by factory name.
     * 
     * @param name - Factory name.
     * @returns PlayFrameworkSettings instance.
     * @throws Error if factory not configured.
     */
    public static getSettings(name: string): PlayFrameworkSettings {
        const settings = this.settings.get(name);

        if (!settings) {
            throw new Error(`PlayFramework settings '${name}' not configured. Call PlayFrameworkServices.configure(name, baseUrl) first.`);
        }

        return settings;
    }

    /**
     * Check if a factory is configured.
     * 
     * @param name - Factory name.
     * @returns True if configured, false otherwise.
     */
    public static isConfigured(name: string): boolean {
        return this.clients.has(name);
    }

    /**
     * Remove a configured factory.
     * 
     * @param name - Factory name.
     */
    public static remove(name: string): void {
        this.clients.delete(name);
        this.settings.delete(name);
    }

    /**
     * Clear all configured factories.
     */
    public static clear(): void {
        this.clients.clear();
        this.settings.clear();
    }

    /**
     * Get the default (first configured) client.
     * Useful when only one factory is configured.
     * 
     * @returns PlayFrameworkClient instance.
     * @throws Error if no factory is configured.
     */
    public static getDefaultClient(): PlayFrameworkClient {
        const first = this.clients.values().next();
        if (first.done) {
            throw new Error("No PlayFramework client configured. Call PlayFrameworkServices.configure(name, baseUrl) first.");
        }
        return first.value;
    }

    /**
     * Get client by optional name. If name is omitted, returns the default (first configured) client.
     * 
     * @param name - Optional factory name.
     * @returns PlayFrameworkClient instance.
     */
    public static resolve(name?: string): PlayFrameworkClient {
        return name ? this.getClient(name) : this.getDefaultClient();
    }
}
