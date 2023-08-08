export type State<T, TKey> = {
    isOk: boolean;
    e: {
        key: TKey;
        value: T;
    },
    c: number | null,
    m: string | null
}