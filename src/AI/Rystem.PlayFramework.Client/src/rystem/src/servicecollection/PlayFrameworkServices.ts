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
     * 
     * @param name - Factory name (unique identifier).
     * @param baseUrl - Base API URL (e.g., "https://api.example.com/api/ai").
     * @param configure - Optional configuration callback.
     * @returns PlayFrameworkSettings instance for further configuration.
     */
    public static configure(
        name: string,
        baseUrl: string,
        configure?: (settings: PlayFrameworkSettings) => void | Promise<void>
    ): PlayFrameworkSettings {
        const settings = new PlayFrameworkSettings(name, baseUrl);

        if (configure) {
            const result = configure(settings);
            if (result instanceof Promise) {
                result.then(() => {
                    this.settings.set(name, settings);
                    this.clients.set(name, new PlayFrameworkClient(settings));
                });
                return settings;
            }
        }

        this.settings.set(name, settings);
        this.clients.set(name, new PlayFrameworkClient(settings));
        return settings;
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
}
