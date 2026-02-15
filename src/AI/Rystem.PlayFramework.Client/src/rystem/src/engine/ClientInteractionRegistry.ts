import { ClientInteractionRequest } from "../models/ClientInteractionRequest";
import { ClientInteractionResult, AIContent } from "../models/ClientInteractionResult";

/**
 * Type for client-side tool implementation.
 * Takes optional arguments and returns AIContent array.
 */
export type ClientTool<TArgs = any> = (args?: TArgs) => Promise<AIContent[]>;

/**
 * Registry for client-side tools (camera, geolocation, file picker, etc.).
 * Tools are registered once and executed when server requests them.
 */
export class ClientInteractionRegistry {
    private tools: Map<string, ClientTool> = new Map();

    /**
     * Registers a client-side tool.
     * @param toolName - Unique tool name matching server OnClient() registration.
     * @param implementation - Async function that executes the tool.
     * 
     * @example
     * registry.register("capturePhoto", async () => {
     *     const stream = await navigator.mediaDevices.getUserMedia({ video: true });
     *     // ... capture frame and return Base64
     *     return [{ type: "data", data: base64, mediaType: "image/jpeg" }];
     * });
     */
    public register<TArgs = any>(toolName: string, implementation: ClientTool<TArgs>): void {
        if (this.tools.has(toolName)) {
            console.warn(`ClientInteractionRegistry: Tool "${toolName}" is already registered. Overwriting.`);
        }
        this.tools.set(toolName, implementation as ClientTool);
    }

    /**
     * Executes a registered tool and returns the result.
     * @param request - Client interaction request from server.
     * @returns ClientInteractionResult with contents or error.
     * 
     * @example
     * const result = await registry.execute(request);
     * // Send result back to server with continuation token
     */
    public async execute(request: ClientInteractionRequest): Promise<ClientInteractionResult> {
        const startTime = Date.now();

        try {
            const tool = this.tools.get(request.toolName);

            if (!tool) {
                return {
                    interactionId: request.interactionId,
                    contents: [],
                    error: `Tool "${request.toolName}" not found in registry. Did you forget to register it?`,
                    executedAt: new Date().toISOString()
                };
            }

            // Execute tool with timeout
            const timeoutPromise = new Promise<never>((_, reject) => {
                setTimeout(() => {
                    reject(new Error(`Tool execution timeout after ${request.timeoutSeconds}s`));
                }, request.timeoutSeconds * 1000);
            });

            const executionPromise = tool(request.arguments);
            const contents = await Promise.race([executionPromise, timeoutPromise]);

            return {
                interactionId: request.interactionId,
                contents,
                executedAt: new Date().toISOString()
            };
        } catch (error: any) {
            return {
                interactionId: request.interactionId,
                contents: [],
                error: error.message || "Unknown error during tool execution",
                executedAt: new Date().toISOString()
            };
        }
    }

    /**
     * Checks if a tool is registered.
     */
    public has(toolName: string): boolean {
        return this.tools.has(toolName);
    }

    /**
     * Gets all registered tool names.
     */
    public getToolNames(): string[] {
        return Array.from(this.tools.keys());
    }

    /**
     * Unregisters a tool.
     */
    public unregister(toolName: string): boolean {
        return this.tools.delete(toolName);
    }

    /**
     * Clears all registered tools.
     */
    public clear(): void {
        this.tools.clear();
    }
}
