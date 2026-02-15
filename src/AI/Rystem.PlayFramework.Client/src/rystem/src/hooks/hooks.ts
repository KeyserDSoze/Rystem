import { PlayFrameworkClient } from "../engine/PlayFrameworkClient";
import { PlayFrameworkServices } from "../servicecollection/PlayFrameworkServices";

/**
 * Get a PlayFrameworkClient instance by optional factory name.
 * 
 * If `name` is provided, returns the client for that specific factory.
 * If `name` is omitted, returns the default (first configured) client.
 * 
 * Framework-agnostic — works with React, Vue, Angular, Svelte, or plain TypeScript.
 * 
 * @param name - Optional factory name. Omit to use the default client.
 * @returns PlayFrameworkClient instance.
 * 
 * @example
 * ```ts
 * // Use default (first configured) client
 * const client = usePlayFramework();
 * 
 * // Use a specific factory
 * const chatClient = usePlayFramework("chat");
 * 
 * // Execute
 * for await (const step of client.executeStepByStep({ message: "Hello" })) {
 *   console.log(step.message);
 * }
 * ```
 */
export function usePlayFramework(name?: string): PlayFrameworkClient {
    return PlayFrameworkServices.resolve(name);
}
