/**
 * Execution phase enum (matches C# ExecutionPhase).
 * Serialized as camelCase from JsonStringEnumConverter.
 */
export type ExecutionPhase =
    | "notStarted"
    | "initialized"
    | "sceneSelected"
    | "executingScene"
    | "awaitingClient"
    | "sceneCompleted"
    | "chaining"
    | "generatingFinalResponse"
    | "finalResponse"
    | "completed"
    | "completedNoResponse"
    | "budgetExceeded"
    | "sceneNotFound"
    | "tooManyToolRequests"
    | "break"
    | "unauthorized";

/**
 * Execution state for resuming conversations.
 */
export interface ExecutionState {
    /**
     * Current execution phase.
     */
    phase: ExecutionPhase;

    /**
     * Executed scenes in order.
     */
    executedSceneOrder: string[];

    /**
     * Tools executed per scene.
     */
    executedScenes: Record<string, any[]>;

    /**
     * Executed tools.
     */
    executedTools: string[];

    /**
     * Accumulated cost.
     */
    accumulatedCost: number;

    /**
     * Current scene name (if resuming mid-execution).
     */
    currentSceneName?: string | null;
}
