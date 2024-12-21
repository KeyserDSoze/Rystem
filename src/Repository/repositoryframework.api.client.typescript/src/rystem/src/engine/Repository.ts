import { CancellationToken } from "typescript";
import { IRepository } from "../interfaces/IRepository";
import { Entity } from "../models/Entity";
import { RepositoryEndpoint } from "../models/RepositoryEndpoint";
import { State } from "../models/State";
import { RepositorySettings } from "../servicecollection/RepositorySettings";
import { BatchBuilder } from "./batch/BatchBuilder";
import { QueryBuilder } from "./query/QueryBuilder";

export class Repository<T, TKey> implements IRepository<T, TKey>
{
    private baseUri: string | null;
    private settings: RepositorySettings;

    constructor(baseUri: string | null, settings: RepositorySettings) {
        if (settings.uri == null)
            this.baseUri = `${baseUri}${settings.path}`;
        else
            this.baseUri = settings.uri;
        this.settings = settings;
    }
    async makeRequest<TResponse>(
        endpoint: RepositoryEndpoint,
        path: string,
        method: string,
        body: any = null,
        headers: HeadersInit = {} as HeadersInit): Promise<TResponse> {
        let retry: boolean = true;
        let response: TResponse = null!;
        const uri: string = `${this.baseUri}/${path}`;
        while (retry) {
            response = await fetch(uri,
                {
                    method: method,
                    headers: this.settings.enrichHeaders(endpoint, uri, method, headers, body),
                    body: body == null ? null : JSON.stringify(body)
                })
                .then(res => {
                    const json = res.json();
                    return json;
                })
                .catch(err => {
                    retry = this.settings.manageError(endpoint, uri, method, headers, body, err);
                    return null;
                })
                .then(res => {
                    retry = false;
                    return res as TResponse;
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
        headers: HeadersInit = {} as HeadersInit,
        cancellationToken: CancellationToken | null = null): Promise<Array<TResponse>> {
        const uri: string = `${this.baseUri}/${path}/Stream`;
        let response: Array<TResponse> = [];
        await fetch(uri,
            {
                method: method,
                headers: this.settings.enrichHeaders(endpoint, uri, method, headers, body),
                body: body == null ? null : JSON.stringify(body)
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
                            response.push(entity);
                            reader(entity);
                        });
                    }
                    bodyReader.releaseLock();
                }
            })
            .catch(async err => {
                console.log(err);
                console.log("in errordalsjdlasjdlasjdlas");
                this.settings.manageError(endpoint, uri, method, headers, body, err);
                return null;
            })
        return response;
    }
    get(key: TKey): Promise<T> {
        if (!this.settings.complexKey) {
            return this.makeRequest<T>(RepositoryEndpoint.Get,
                `Get?key=${key}`, 'GET');
        } else {
            return this.makeRequest<T>(RepositoryEndpoint.Get, `Get`, 'POST',
                key,
                {
                    'content-type': 'application/json;charset=UTF-8',
                });
        }
    }
    insert(key: TKey, value: T): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Insert, `Insert?key=${key}`, 'POST',
                value,
                {
                    'content-type': 'application/json;charset=UTF-8',
                });
        } else {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Insert, `Insert`, 'POST',
                {
                    value: value,
                    key: key
                } as Entity<T, TKey>,
                {
                    'content-type': 'application/json;charset=UTF-8',
                });
        }
    }
    update(key: TKey, value: T): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Update,
                `Update?key=${key}`, 'POST',
                value,
                {
                    'content-type': 'application/json;charset=UTF-8',
                });
        } else {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Update,
                `Update`, 'POST',
                {
                    value: value,
                    key: key
                } as Entity<T, TKey>,
                {
                    'content-type': 'application/json;charset=UTF-8',
                });
        }
    }
    exist(key: TKey): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Exist,
                `Exist?key=${key}`, 'GET');
        } else {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Exist,
                `Exist`, 'POST',
                key,
                {
                    'content-type': 'application/json;charset=UTF-8',
                });
        }
    }
    delete(key: TKey): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Delete,
                `Delete?key=${key}`, 'GET');
        } else {
            return this.makeRequest<State<T, TKey>>(RepositoryEndpoint.Delete,
                `Delete`, 'POST',
                key,
                {
                    'content-type': 'application/json;charset=UTF-8',
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

    public decodeChunk<T>(
        value: Uint8Array,
        decodedItemCallback: (item: T) => void): void {
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
