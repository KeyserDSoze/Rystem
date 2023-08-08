import { State } from "../../models/State";

export type BatchResult<T, TKey> = {
    c: 1 | 2 | 4;
    k: TKey;
    s: State<T, TKey>;
};
