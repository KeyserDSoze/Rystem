import { State, SerializedState } from "../../models/State";

export type BatchResult<T, TKey> = {
    code: 1 | 2 | 4;
    key: TKey;
    state: State<T, TKey>;
};

export type SerializedBatchResult<T, TKey> = {
    c: 1 | 2 | 4;
    k: TKey;
    s: SerializedState<T, TKey>;
};

export function isBatchResult<T, TKey>(obj: any): obj is BatchResult<T, TKey> {
    return (
        typeof obj === 'object' &&
        obj !== null &&
        (obj.code === 1 || obj.code === 2 || obj.code === 4) &&
        'key' in obj &&
        'state' in obj
    );
}

export function isSerializedBatchResult<T, TKey>(obj: any): obj is SerializedBatchResult<T, TKey> {
    return (
        typeof obj === 'object' &&
        obj !== null &&
        (obj.c === 1 || obj.c === 2 || obj.c === 4) &&
        'k' in obj &&
        's' in obj
    );
}