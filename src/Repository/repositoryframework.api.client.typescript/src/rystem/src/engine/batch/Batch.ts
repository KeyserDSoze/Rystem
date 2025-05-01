import { BatchOperation, isBatchOperation, isSerializedBatchOperation } from "./BatchOperation";

export type Batch<T, TKey> = {
    values: Array<BatchOperation<T, TKey>>;
};

export type SerializedBatch<T, TKey> = {
    v: Array<BatchOperation<T, TKey>>;
};

export function isBatch<T, TKey>(obj: any): obj is Batch<T, TKey> {
    return (
        typeof obj === 'object' &&
        obj !== null &&
        Array.isArray(obj.values) &&
        obj.values.every(isBatchOperation)
    );
}

export function isSerializedBatch<T, TKey>(obj: any): obj is SerializedBatch<T, TKey> {
    return (
        typeof obj === 'object' &&
        obj !== null &&
        Array.isArray(obj.v) &&
        obj.v.every(isSerializedBatchOperation)
    );
}