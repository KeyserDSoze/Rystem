import type { StoredMessage } from './StoredMessage';
import type { ExecutionState } from './ExecutionState';

/**
 * Stored conversation with messages and metadata.
 */
export interface StoredConversation {
    /**
     * Unique conversation identifier.
     */
    conversationKey: string;

    /**
     * User who owns this conversation (null = public).
     */
    userId?: string | null;

    /**
     * Whether this conversation is public (accessible to everyone).
     */
    isPublic: boolean;

    /**
     * Last update timestamp.
     */
    timestamp: string; // ISO 8601 format

    /**
     * Serializable messages.
     */
    messages: StoredMessage[];

    /**
     * Execution state (scenes executed, tools used, etc.).
     */
    executionState?: ExecutionState | null;
}

/**
 * Query parameters for listing conversations.
 */
export interface ConversationQueryParameters {
    /**
     * Search text in conversation messages (optional).
     */
    searchText?: string;

    /**
     * Sort order by timestamp.
     */
    orderBy?: ConversationSortOrder;

    /**
     * Include public conversations (default: true).
     */
    includePublic?: boolean;

    /**
     * Include private conversations owned by current user (default: true).
     */
    includePrivate?: boolean;

    /**
     * Number of items to skip for pagination (default: 0).
     */
    skip?: number;

    /**
     * Number of items to take for pagination (default: 50).
     */
    take?: number;
}

/**
 * Sort order for conversation list.
 */
export enum ConversationSortOrder {
    /**
     * Newest first (default).
     */
    TimestampDescending = 0,

    /**
     * Oldest first.
     */
    TimestampAscending = 1
}

/**
 * Request body for updating conversation visibility.
 */
export interface UpdateConversationVisibilityRequest {
    /**
     * Whether the conversation should be public.
     */
    isPublic: boolean;
}
