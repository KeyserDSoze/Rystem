import { PlayFrameworkClient } from "../engine/PlayFrameworkClient";
import { PlayFrameworkServices } from "../servicecollection/PlayFrameworkServices";

/**
 * React hook to get PlayFrameworkClient by factory name.
 * 
 * @param name - Factory name (must be configured via PlayFrameworkServices.configure).
 * @returns PlayFrameworkClient instance.
 * 
 * @example
 * ```tsx
 * const client = usePlayFramework("chat");
 * 
 * for await (const step of client.executeStepByStep({ prompt: "Hello" })) {
 *   console.log(step.message);
 * }
 * ```
 */
export const usePlayFramework = (name: string): PlayFrameworkClient => {
    return PlayFrameworkServices.getClient(name);
};
