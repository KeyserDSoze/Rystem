import { BatchElement } from "./BatchElement";
import { Batch } from "./Batch";
import { BatchResults } from "./BatchResults";


export class BatchBuilder<T, TKey> {
    private batch: Batch<T, TKey>;
    private baseUri: string | null;

    constructor(baseUri: string | null) {
        this.batch = {
            v: [] as Array<BatchElement<T, TKey>>
        } as Batch<T, TKey>;
        this.baseUri = baseUri;
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
        return fetch(`${this.baseUri}/Batch`,
            {
                method: 'POST',
                headers: {
                    'content-type': 'application/json;charset=UTF-8',
                },
                body: JSON.stringify(this.batch),
            })
            .then(res => {
                const json = res.json();
                return json;
            })
            .catch((err) => {
                return {} as BatchResults<T, TKey>;
            })
            .then(res => {
                return res as BatchResults<T, TKey>;
            })
    }
}
