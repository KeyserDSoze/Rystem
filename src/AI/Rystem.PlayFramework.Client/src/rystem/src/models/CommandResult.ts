/**
 * Result of a client command execution.
 * Commands are fire-and-forget client tools that don't require immediate response.
 */
export interface CommandResult {
    /**
     * Indicates if the command executed successfully.
     */
    success: boolean;

    /**
     * Optional message to send to the LLM (e.g., error details, confirmation message).
     * If undefined, only the success status is sent.
     */
    message?: string;
}

/**
 * Helper methods for creating CommandResult instances.
 */
export const CommandResult = {
    /**
     * Creates a successful result.
     * @param message - Optional success message
     */
    ok: (message?: string): CommandResult => ({ success: true, message }),

    /**
     * Creates a failed result.
     * @param message - Error message describing what went wrong
     */
    fail: (message: string): CommandResult => ({ success: false, message }),
};
