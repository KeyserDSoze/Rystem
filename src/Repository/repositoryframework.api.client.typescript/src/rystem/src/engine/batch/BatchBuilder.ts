import { BatchElement } from "./BatchElement";
import { Batch } from "./Batch";
import { Repository } from "../Repository";
import { RepositoryEndpoint } from "../../models/RepositoryEndpoint";
import { BatchResult } from "./BatchResult";
import { CancellationToken } from "typescript";


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
    execute(): Promise<Array<BatchResult<T, TKey>>> {
        return this.repository.makeRequest<Array<BatchResult<T, TKey>>>(RepositoryEndpoint.Batch,
            `Batch`, 'POST',
            this.batch,
            {
                'content-type': 'application/json;charset=UTF-8',
            });
    }
    executeAsStream(entityReader: (entity: BatchResult<T, TKey>) => void,
        cancellationToken: CancellationToken | null = null): Promise<Array<BatchResult<T, TKey>>> {
        return this.repository.makeRequestAsStream<BatchResult<T, TKey>>(
            RepositoryEndpoint.BatchStream,
            `Batch`, 'POST',
            entityReader,
            this.batch,
            {
                'content-type': 'application/json;charset=UTF-8',
            }, cancellationToken);
    }
}
