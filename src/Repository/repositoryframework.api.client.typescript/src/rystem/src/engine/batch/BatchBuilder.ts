import { BatchElement } from "./BatchElement";
import { Batch } from "./Batch";
import { BatchResults } from "./BatchResults";
import { Repository } from "../Repository";


export class BatchBuilder<T, TKey> {
    private batch: Batch<T, TKey>;
    private repository: Repository<T, TKey>;

    constructor(repository: Repository<T, TKey>) {
        this.batch = {
            v: [] as Array<BatchElement<T, TKey>>
        } as Batch<T, TKey>;
        this.repository = repository;
    }
    addInsert(key: TKey, value: T): BatchBuilder<T, TKey> {
        this.batch.v.push({
            c: 1,
            k: key,
            v: value
        } as BatchElement<T, TKey>);
        return this;
    }
    addUpdate(key: TKey, value: T): BatchBuilder<T, TKey> {
        this.batch.v.push({
            c: 2,
            k: key,
            v: value
        } as BatchElement<T, TKey>);
        return this;
    }
    addDelete(key: TKey): BatchBuilder<T, TKey> {
        this.batch.v.push({
            c: 4,
            k: key
        } as BatchElement<T, TKey>);
        return this;
    }
    execute(): Promise<BatchResults<T, TKey>> {
        return this.repository.makeRequest<BatchResults<T, TKey>>(`Batch`, 'POST',
            JSON.stringify(this.batch),
            {
                'content-type': 'application/json;charset=UTF-8',
            });
    }
}
