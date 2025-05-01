export type Entity<T, TKey> = {
    value: T;
    key: TKey;
}

export type SerializedEntity<T, TKey> = {
    v: T;
    k: TKey;
}
export function isEntity<T, TKey>(obj: any): obj is Entity<T, TKey> {
    return (
        typeof obj === 'object' &&
        obj !== null &&
        'value' in obj &&
        'key' in obj
    );
}
export function isSerializedEntity<T, TKey>(obj: any): obj is SerializedEntity<T, TKey> {
    return (
        typeof obj === 'object' &&
        obj !== null &&
        'v' in obj &&
        'k' in obj
    );
}

export function isSerializedEntityArray<T, TKey>(arr: any): arr is SerializedEntity<T, TKey>[] {
    return Array.isArray(arr) && arr.every(isSerializedEntity);
}