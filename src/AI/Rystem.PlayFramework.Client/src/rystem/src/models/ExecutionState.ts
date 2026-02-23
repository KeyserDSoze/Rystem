/**
 * Execution state for resuming conversations.
 */
export interface ExecutionState {
    /**
     * Current execution phase.
     */
    phase: string;

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
