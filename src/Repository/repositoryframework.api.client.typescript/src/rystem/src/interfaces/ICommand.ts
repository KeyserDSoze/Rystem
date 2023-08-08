import { BatchBuilder } from "../engine/batch/BatchBuilder";
import { State } from "../models/State";

export interface ICommand<T, TKey> {
    insert(key: TKey, value: T): Promise<State<T, TKey>>;
    update(key: TKey, value: T): Promise<State<T, TKey>>;
    delete(key: TKey): Promise<State<T, TKey>>;
    batch(): BatchBuilder<T, TKey>;
}
