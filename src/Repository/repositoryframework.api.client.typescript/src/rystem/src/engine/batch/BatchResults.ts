import { BatchResult } from "./BatchResult";

export type BatchResults<T, TKey> = {
    r: Array<BatchResult<T, TKey>>;
};
