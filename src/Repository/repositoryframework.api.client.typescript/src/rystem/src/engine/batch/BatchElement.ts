export type BatchElement<T, TKey> = {
    c: 1 | 2 | 4;
    k: TKey;
    v: T | null;
};
