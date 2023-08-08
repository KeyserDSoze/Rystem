import { IRepository } from "../interfaces/IRepository";
import { Entity } from "../models/Entity";
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
        path: string,
        method: string,
        body: BodyInit | null = null,
        headers: HeadersInit = {} as HeadersInit): Promise<TResponse> {
        let retry: boolean = true;
        let response: TResponse = null!;
        while (retry) {
            response = await fetch(`${this.baseUri}/${path}`,
                {
                    method: method,
                    headers: this.settings.enrichHeaders(headers),
                    body: body
                })
                .then(res => {
                    const json = res.json();
                    return json;
                })
                .catch((err) => {
                    retry = this.settings.manageError(err);
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
    get(key: TKey): Promise<T> {
        if (!this.settings.complexKey) {
            return this.makeRequest<T>(
                `Get?key=${key}`, 'GET');
        } else {
            return this.makeRequest<T>(`Get`, 'POST',
                JSON.stringify(key),
                {
                    'content-type': 'application/json;charset=UTF-8',
                });
        }
    }
    insert(key: TKey, value: T): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return this.makeRequest<State<T, TKey>>(`Insert?key=${key}`, 'POST',
                JSON.stringify(value),
                {
                    'content-type': 'application/json;charset=UTF-8',
                });
        } else {
            return this.makeRequest<State<T, TKey>>(`Insert`, 'POST',
                JSON.stringify({
                    value: value,
                    key: key
                } as Entity<T, TKey>),
                {
                    'content-type': 'application/json;charset=UTF-8',
                });
        }
    }
    update(key: TKey, value: T): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return this.makeRequest<State<T, TKey>>(`Update?key=${key}`, 'POST',
                JSON.stringify(value),
                {
                    'content-type': 'application/json;charset=UTF-8',
                });
        } else {
            return this.makeRequest<State<T, TKey>>(`Update`, 'POST',
                JSON.stringify({
                    value: value,
                    key: key
                } as Entity<T, TKey>),
                {
                    'content-type': 'application/json;charset=UTF-8',
                });
        }
    }
    exist(key: TKey): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return this.makeRequest<State<T, TKey>>(`Exist?key=${key}`, 'GET');
        } else {
            return this.makeRequest<State<T, TKey>>(`Exist`, 'POST',
                JSON.stringify(key),
                {
                    'content-type': 'application/json;charset=UTF-8',
                });
        }
    }
    delete(key: TKey): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return this.makeRequest<State<T, TKey>>(`Delete?key=${key}`, 'GET');
        } else {
            return this.makeRequest<State<T, TKey>>(`Delete`, 'POST',
                JSON.stringify(key),
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