/**
 * Serializable representation of a message.
 */
export interface StoredMessage {
    /**
     * Business type flags.
     */
    businessType: number;

    /**
     * Label for debugging.
     */
    label?: string | null;

    /**
     * The role of the message (User, Assistant, System, Tool).
     */
    role: string;

    /**
     * Text content of the message (if any).
     */
    text?: string | null;

    /**
     * Serialized contents for complex messages.
     */
    contents?: any[] | null;

    /**
     * Additional properties from the original message.
     */
    additionalProperties?: Record<string, any> | null;
}
