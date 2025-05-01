import { CancellationToken } from "typescript";
import { IRepository } from "../interfaces/IRepository";
import { Entity, isEntity, isSerializedEntity, isSerializedEntityArray } from "../models/Entity";
import { RepositoryEndpoint } from "../models/RepositoryEndpoint";
import { State, isState, isSerializedState } from "../models/State";
import { RepositorySettings } from "../servicecollection/RepositorySettings";
import { BatchBuilder } from "./batch/BatchBuilder";
import { isBatch, isSerializedBatch } from "./batch/Batch";
import { isSerializedBatchResult } from "./batch/BatchResult";
import { QueryBuilder } from "./query/QueryBuilder";
export class Repository<T, TKey> implements IRepository<T, TKey> {
    private baseUri: string | null;
    private settings: RepositorySettings;

    constructor(baseUri: string | null, settings: RepositorySettings) {
        if (settings.uri == null)
            this.baseUri = `${baseUri}${settings.path}`;
        else
            this.baseUri = settings.uri;
        this.settings = settings;
    }
    private serializeRequestBody(body: any, isKey: boolean): any {
        const { transformer, keyTransformer } = this.settings;

        if (!body)
            return body;

        // Entity<T, TKey> → SerializedEntity<T, TKey>
        if (isEntity(body)) {
            return {
                v: transformer?.toPlain?.(body.value) ?? body.value,
                k: keyTransformer?.toPlain?.(body.key) ?? body.key,
            };
        }

        // Batch<T, TKey> → SerializedBatch<T, TKey>
        if (isBatch(body)) {
            return {
                v: body.values.map(op => ({
                    c: op.command,
                    k: op.key && keyTransformer?.toPlain ? keyTransformer.toPlain(op.key) : op.key,
                    v: op.value && transformer?.toPlain ? transformer.toPlain(op.value) : op.value,
                })),
            };
        }

        // State<T, TKey> → SerializedState<T, TKey>
        if (isState(body)) {
            return {
                i: body.isOk,
                c: body.code,
                m: body.message,
                e: {
                    k: keyTransformer?.toPlain?.(body.entity.key) ?? body.entity.key,
                    v: transformer?.toPlain?.(body.entity.value) ?? body.entity.value,
                },
            };
        }

        // Fallback — assume it's T
        if (!isKey)
            return transformer?.toPlain?.(body) ?? body;
        else
            return keyTransformer?.toPlain?.(body) ?? body;
    }


    private transformResponse<TResponse>(res: any): TResponse {
        const { transformer, keyTransformer } = this.settings;

        if (!res)
            return res;

        // SerializedState<T, TKey> → State<T, TKey>
        if (isSerializedState(res)) {
            return {
                isOk: res.i,
                code: res.c,
                message: res.m,
                entity: res.e ? {
                    key: res.e.k && keyTransformer?.fromPlain ? keyTransformer.fromPlain(res.e.k) : res.e.k,
                    value: res.e.v && transformer?.fromPlain ? transformer.fromPlain(res.e.v) : res.e.v,
                } : res.e,
            } as TResponse;
        }

        // SerializedEntity<T, TKey> → Entity<T, TKey>
        if (isSerializedEntity(res)) {
            return {
                key: res.k && keyTransformer?.fromPlain ? keyTransformer.fromPlain(res.k) : res.k,
                value: res.v && transformer?.fromPlain ? transformer.fromPlain(res.v) : res.v,
            } as TResponse;
        }

        // SerializedEntity<T, TKey>[] → Entity<T, TKey>[]
        if (isSerializedEntityArray(res)) {
            return res.map(item => ({
                key: item.k && keyTransformer?.fromPlain ? keyTransformer.fromPlain(item.k) : item.k,
                value: item.v && transformer?.fromPlain ? transformer.fromPlain(item.v) : item.v,
            })) as TResponse;
        }

        // SerializedBatchResult<T, TKey>[] → BatchResult<T, TKey>[]
        if (Array.isArray(res) && res.every(isSerializedBatchResult)) {
            return res.map(item => ({
                code: item.c,
                key: item.k && keyTransformer?.fromPlain ? keyTransformer.fromPlain(item.k) : item.k,
                state: item.s ? {
                    isOk: item.s.i,
                    code: item.s.c,
                    message: item.s.m,
                    entity: item.s.e ? {
                        key: item.s.e.k && keyTransformer?.fromPlain ? keyTransformer.fromPlain(item.s.e.k) : item.s.e.k,
                        value: item.s.e.v && transformer?.fromPlain ? transformer.fromPlain(item.s.e.v) : item.s.e.v,
                    } : item.s.e,
                } : item.s,
            })) as TResponse;
        }

        // SerializedBatch<T, TKey> → Batch<T, TKey>
        if (isSerializedBatch(res)) {
            return {
                values: res.v.map((op: any) => ({
                    command: op.c,
                    key: op.k && keyTransformer?.fromPlain ? keyTransformer.fromPlain(op.k) : op.k,
                    value: op.v && transformer?.fromPlain ? transformer.fromPlain(op.v) : op.v,
                })),
            } as TResponse;
        }

        // SerializedBatchResult<T, TKey> → BatchResult<T, TKey>
        if (isSerializedBatchResult(res)) {
            return {
                code: res.c,
                key: keyTransformer?.fromPlain?.(res.k) ?? res.k,
                state: res.s ? {
                    isOk: res.s.i,
                    code: res.s.c,
                    message: res.s.m,
                    entity: res.s.e ? {
                        key: res.s.e.k && keyTransformer?.fromPlain ? keyTransformer.fromPlain(res.s.e.k) : res.s.e.k,
                        value: res.s.e.v && transformer?.fromPlain ? transformer.fromPlain(res.s.e.v) : res.s.e.v,
                    } : res.s.e,
                } : res.s,
            } as TResponse;
        }

        // Fallback: T ← fromPlain
        return transformer?.fromPlain?.(res) ?? res;
    }


    async makeRequest<TResponse>(
        endpoint: RepositoryEndpoint,
        path: string,
        method: string,
        body: any = null,
        bodyIsKey: boolean = false,
        skipSerialization: boolean = false,
        skipSerializationOnResponse: boolean = false,
        headers: HeadersInit = {} as HeadersInit
    ): Promise<TResponse> {
        let retry: boolean = true;
        let response: TResponse = null!;
        const uri: string = `${this.baseUri}/${path}`;
        while (retry) {
            response = await fetch(uri, {
                method: method,
                headers: this.settings.enrichHeaders(endpoint, uri, method, headers, body),
                body: body == null ? null : JSON.stringify(skipSerialization ? body : this.serializeRequestBody(body, bodyIsKey))
            })
                .then(async res => {
                    const json = await res.json();
                    if (!res.ok)
                        throw json;
                    return json;
                })
                .catch((err: any): null => {
                    retry = this.settings.manageError(endpoint, uri, method, headers, body, err);
                    return null;
                })
                .then(res => {
                    retry = false;
                    return skipSerializationOnResponse ? res : this.transformResponse<TResponse>(res);
                });
            if (retry)
                await new Promise(f => setTimeout(f, 200));
        }
        return response;
    }

    async makeRequestAsStream<TResponse>(
        endpoint: RepositoryEndpoint,
        path: string,
        method: string,
        reader: (entity: TResponse) => void,
        body: any = null,
        bodyIsKey: boolean = false,
        skipSerialization: boolean = false,
        skipSerializationOnResponse: boolean = false,
        headers: HeadersInit = {} as HeadersInit,
        cancellationToken: CancellationToken | null = null
    ): Promise<Array<TResponse>> {
        const uri: string = `${this.baseUri}/${path}/Stream`;
        let response: Array<TResponse> = [];
        await fetch(uri, {
            method: method,
            headers: this.settings.enrichHeaders(endpoint, uri, method, headers, body),
            body: body == null ? null : JSON.stringify(skipSerialization ? body : this.serializeRequestBody(body, bodyIsKey))
        })
            .then(async res => {
                const bodyReader = res.body?.getReader();
                const decoder = new JsonStreamDecoder();
                if (bodyReader != undefined) {
                    while (true) {
                        const { done, value } = await bodyReader.read();
                        if (done)
                            break;
                        if (!value)
                            continue;
                        if (cancellationToken?.isCancellationRequested())
                            break;
                        decoder.decodeChunk<TResponse>(value, entity => {
                            const transformed = skipSerializationOnResponse ? entity : this.transformResponse<TResponse>(entity);
                            response.push(transformed);
                            reader(transformed);
                        });
                    }
                    bodyReader.releaseLock();
                }
            })
            .catch(async (err): Promise<null> => {
                this.settings.manageError(endpoint, uri, method, headers, body, err);
                return null;
            });
        return response;
    }

    get(key: TKey): Promise<T> {
        if (!this.settings.complexKey) {
            return this.makeRequest<T>(RepositoryEndpoint.Get, `Get?key=${key}`, 'GET');
        } else {
            return this.makeRequest<T>(RepositoryEndpoint.Get, `Get`, 'POST', key, true, false, false, {
                'content-type': 'application/json;charset=UTF-8'
            });
        }
    }

    insert(key: TKey, value: T): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Insert, `Insert?key=${key}`, 'POST', {
                value: value,
                key: key
            } as Entity<T, TKey>, false, false, false, {
                'content-type': 'application/json;charset=UTF-8'
            });
        } else {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Insert, `Insert`, 'POST', {
                value: value,
                key: key
            } as Entity<T, TKey>, false, false, false, {
                'content-type': 'application/json;charset=UTF-8'
            });
        }
    }

    update(key: TKey, value: T): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Update, `Update?key=${key}`, 'POST', {
                value: value,
                key: key
            } as Entity<T, TKey>, false, false, false, {
                'content-type': 'application/json;charset=UTF-8'
            });
        } else {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Update, `Update`, 'POST', {
                value: value,
                key: key
            } as Entity<T, TKey>, false, false, false, {
                'content-type': 'application/json;charset=UTF-8'
            });
        }
    }

    exist(key: TKey): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Exist, `Exist?key=${key}`, 'GET');
        } else {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Exist, `Exist`, 'POST', key, true, false, false, {
                'content-type': 'application/json;charset=UTF-8'
            });
        }
    }

    delete(key: TKey): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Delete, `Delete?key=${key}`, 'GET');
        } else {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Delete, `Delete`, 'POST', key, true, false, false, {
                'content-type': 'application/json;charset=UTF-8'
            });
        }
    }

    batch(): BatchBuilder<T, TKey> {
        return new BatchBuilder<T, TKey>(this);
    }

    query(): QueryBuilder<T, TKey> {
        return new QueryBuilder<T, TKey>(this);
    }

    public static predicateAsString<T>(predicate: (value: T) => any): string {
        const splittedPredicate = predicate.toString().split('.');
        const variableName = splittedPredicate.slice(-(splittedPredicate.length - 1)).join('.');
        return `_rystem.${variableName}`;
    }
}

class JsonStreamDecoder {
    private level = 0;
    private partialItem = '';
    private static JTOKEN_START_OBJECT = '{';
    private static JTOKEN_END_OBJECT = '}';
    private decoder = new TextDecoder();

    public decodeChunk<T>(value: Uint8Array, decodedItemCallback: (item: T) => void): void {
        const chunk = this.decoder.decode(value);
        let itemStart = 0;

        for (let i = 0; i < chunk.length; i++) {
            if (chunk[i] === JsonStreamDecoder.JTOKEN_START_OBJECT) {
                if (this.level === 0) {
                    itemStart = i;
                }
                this.level++;
            }
            if (chunk[i] === JsonStreamDecoder.JTOKEN_END_OBJECT) {
                this.level--;
                if (this.level === 0) {
                    let item = chunk.substring(itemStart, i + 1);
                    if (this.partialItem) {
                        item = this.partialItem + item;
                        this.partialItem = '';
                    }
                    decodedItemCallback(JSON.parse(item));
                }
            }
        }
        if (this.level !== 0) {
            this.partialItem = chunk.substring(itemStart);
        }
    }
}