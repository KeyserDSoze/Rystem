import { ClientInteractionRequest } from "../models/ClientInteractionRequest";
import { ClientInteractionResult, AIContent } from "../models/ClientInteractionResult";
import { CommandResult } from "../models/CommandResult";

/**
 * Type for client-side tool implementation.
 * Takes optional arguments and returns AIContent array.
 */
export type ClientTool<TArgs = any> = (args?: TArgs) => Promise<AIContent[]>;

/**
 * Type for client-side command implementation.
 * Takes optional arguments and returns CommandResult (success + optional message).
 */
export type ClientCommand<TArgs = any> = (args?: TArgs) => Promise<CommandResult>;

/**
 * Command feedback modes:
 * - 'never': Never send feedback (silent command)
 * - 'onError': Send feedback only on failure (default)
 * - 'always': Always send feedback (even on success)
 */
export type CommandFeedbackMode = 'never' | 'onError' | 'always';

/**
 * Options for command registration.
 */
export interface CommandOptions {
    /**
     * When to send feedback to the server.
     * @default 'onError'
     */
    feedbackMode?: CommandFeedbackMode;
}

/**
 * Registry for client-side tools (camera, geolocation, file picker, etc.).
 * Tools are registered once and executed when server requests them.
 */
export class ClientInteractionRegistry {
    private tools: Map<string, ClientTool | ClientCommand> = new Map();
    private commands: Map<string, CommandOptions> = new Map();

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
        this.commands.delete(toolName); // Remove from commands if re-registered as tool
    }

    /**
     * Registers a command (fire-and-forget tool).
     * Commands return CommandResult and can optionally send feedback based on feedbackMode.
     * @param toolName - Unique command name matching server AddCommand() registration.
     * @param implementation - Async function that returns CommandResult.
     * @param options - Feedback configuration.
     * 
     * @example
     * registry.registerCommand("logAction", async (args) => {
     *     console.log(args.action);
     *     return CommandResult.ok();
     * }, { feedbackMode: 'never' });
     */
    public registerCommand<TArgs = any>(
        toolName: string,
        implementation: ClientCommand<TArgs>,
        options?: CommandOptions
    ): void {
        if (this.tools.has(toolName)) {
            console.warn(`ClientInteractionRegistry: Command "${toolName}" is already registered. Overwriting.`);
        }
        this.tools.set(toolName, implementation as ClientCommand);
        this.commands.set(toolName, {
            feedbackMode: options?.feedbackMode ?? 'onError'
        });
    }

    /**
     * Checks if a tool is registered as a command.
     */
    public isCommand(toolName: string): boolean {
        return this.commands.has(toolName);
    }

    /**
     * Gets command options.
     */
    public getCommandOptions(toolName: string): CommandOptions | undefined {
        return this.commands.get(toolName);
    }

    /**
     * Executes a registered tool/command and returns the result.
     * For commands, converts CommandResult to AIContent[] if needed.
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

            const isCommand = this.isCommand(request.toolName);

            // Execute tool with timeout (protects against client-side hangs/crashes)
            const timeoutMs = request.timeoutSeconds > 0 ? request.timeoutSeconds * 1000 : 0;
            const executionPromise = tool(request.arguments);

            let result: AIContent[] | CommandResult;
            if (timeoutMs > 0) {
                let timer: ReturnType<typeof setTimeout> | undefined;
                const timeoutPromise = new Promise<never>((_, reject) => {
                    timer = setTimeout(() => {
                        reject(new Error(`Tool execution timeout after ${request.timeoutSeconds}s`));
                    }, timeoutMs);
                });

                try {
                    result = await Promise.race([executionPromise, timeoutPromise]);
                } finally {
                    if (timer !== undefined) clearTimeout(timer);
                }
            } else {
                // No timeout — wait indefinitely for the tool to complete
                result = await executionPromise;
            }

            // If it's a command, convert CommandResult to AIContent[]
            let contents: AIContent[];
            if (isCommand && 'success' in result) {
                const cmdResult = result as CommandResult;
                const commandOptions = this.getCommandOptions(request.toolName);
                const feedbackMode = commandOptions?.feedbackMode ?? 'onError';

                // Determine if feedback should be sent
                const shouldSendFeedback = 
                    feedbackMode === 'always' ||
                    (feedbackMode === 'onError' && !cmdResult.success);

                if (shouldSendFeedback) {
                    // Send feedback (success + message)
                    contents = [
                        { type: 'text', text: cmdResult.success ? 'true' : 'false' }
                    ];
                    if (cmdResult.message) {
                        contents.push({ type: 'text', text: cmdResult.message });
                    }
                } else {
                    // No feedback mode - send minimal success indicator
                    contents = [{ type: 'text', text: 'true' }];
                }
            } else {
                contents = result as AIContent[];
            }

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
