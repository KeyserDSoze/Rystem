import { BatchElement } from "./BatchElement";

export type Batch<T, TKey> = {
    v: Array<BatchElement<T, TKey>>;
};