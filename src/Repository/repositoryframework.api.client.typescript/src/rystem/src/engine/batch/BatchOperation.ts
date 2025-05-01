export type BatchOperation<T, TKey> = {
    command: 1 | 2 | 4;
    key: TKey;
    value: T | null;
};

export type SerializedBatchOperation<T, TKey> = {
    c: 1 | 2 | 4;
    k: TKey;
    v: T | null;
};

export function isBatchOperation<T, TKey>(obj: any): obj is BatchOperation<T, TKey> {
    return (
        typeof obj === 'object' &&
        obj !== null &&
        (obj.command === 1 || obj.command === 2 || obj.command === 4) &&
        'key' in obj
    );
}

export function isSerializedBatchOperation<T, TKey>(obj: any): obj is SerializedBatchOperation<T, TKey> {
    return (
        typeof obj === 'object' &&
        obj !== null &&
        (obj.c === 1 || obj.c === 2 || obj.c === 4) &&
        'k' in obj
    );
}
