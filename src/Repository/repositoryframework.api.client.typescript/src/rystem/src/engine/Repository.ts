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
    get(key: TKey): Promise<T> {
        if (!this.settings.complexKey) {
            return fetch(`${this.baseUri}/Get?key=${key}`,
                {
                    method: 'GET',
                    headers: {
                    }
                })
                .then(res => {
                    const json = res.json();
                    return json;
                })
                .catch((err) => {
                    return null;
                })
                .then(res => {
                    return res as T;
                });
        } else {
            return fetch(`${this.baseUri}/Get`,
                {
                    method: 'POST',
                    headers: {
                        'content-type': 'application/json;charset=UTF-8',
                    },
                    body: JSON.stringify(key),
                })
                .then(res => {
                    const json = res.json();
                    return json;
                })
                .catch((err) => {
                    return null;
                })
                .then(res => {
                    return res as T;
                });
        }
    }
    insert(key: TKey, value: T): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return fetch(`${this.baseUri}/Insert?key=${key}`,
                {
                    method: 'POST',
                    headers: {
                        'content-type': 'application/json;charset=UTF-8',
                    },
                    body: JSON.stringify(value),
                })
                .then(res => {
                    const json = res.json();
                    return json;
                })
                .catch((err) => {
                    return { isOk: false, m: err } as State<T, TKey>;
                })
                .then(res => {
                    return res as State<T, TKey>;
                });
        } else {
            return fetch(`${this.baseUri}/Insert`,
                {
                    method: 'POST',
                    headers: {
                        'content-type': 'application/json;charset=UTF-8',
                    },
                    body: JSON.stringify({
                        value: value,
                        key: key
                    } as Entity<T, TKey>),
                })
                .then(res => {
                    const json = res.json();
                    return json;
                })
                .catch((err) => {
                    return { isOk: false, m: err } as State<T, TKey>;
                })
                .then(res => {
                    return res as State<T, TKey>;
                });
        }
    }
    update(key: TKey, value: T): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return fetch(`${this.baseUri}/Update?key=${key}`,
                {
                    method: 'POST',
                    headers: {
                        'content-type': 'application/json;charset=UTF-8',
                    },
                    body: JSON.stringify(value),
                })
                .then(res => {
                    const json = res.json();
                    return json;
                })
                .catch((err) => {
                    return { isOk: false, m: err } as State<T, TKey>;
                })
                .then(res => {
                    return res as State<T, TKey>;
                });
        } else {
            return fetch(`${this.baseUri}/Update`,
                {
                    method: 'POST',
                    headers: {
                        'content-type': 'application/json;charset=UTF-8',
                    },
                    body: JSON.stringify({
                        value: value,
                        key: key
                    } as Entity<T, TKey>),
                })
                .then(res => {
                    const json = res.json();
                    return json;
                })
                .catch((err) => {
                    return { isOk: false, m: err } as State<T, TKey>;
                })
                .then(res => {
                    return res as State<T, TKey>;
                });
        }
    }
    exist(key: TKey): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return fetch(`${this.baseUri}/Exist?key=${key}`,
                {
                    method: 'GET',
                    headers: {
                    }
                })
                .then(res => {
                    const json = res.json();
                    return json;
                })
                .catch((err) => {
                    return { isOk: false, m: err } as State<T, TKey>;
                })
                .then(res => {
                    return res as State<T, TKey>;
                });
        } else {
            return fetch(`${this.baseUri}/Exist`,
                {
                    method: 'POST',
                    headers: {
                        'content-type': 'application/json;charset=UTF-8',
                    },
                    body: JSON.stringify(key),
                })
                .then(res => {
                    const json = res.json();
                    return json;
                })
                .catch((err) => {
                    return { isOk: false, m: err } as State<T, TKey>;
                })
                .then(res => {
                    return res as State<T, TKey>;
                });
        }
    }
    delete(key: TKey): Promise<State<T, TKey>> {
        if (!this.settings.complexKey) {
            return fetch(`${this.baseUri}/Delete?key=${key}`,
                {
                    method: 'GET',
                    headers: {
                    }
                })
                .then(res => {
                    const json = res.json();
                    return json;
                })
                .catch((err) => {
                    return { isOk: false, m: err } as State<T, TKey>;
                })
                .then(res => {
                    return res as State<T, TKey>;
                });
        } else {
            return fetch(`${this.baseUri}/Delete`,
                {
                    method: 'POST',
                    headers: {
                        'content-type': 'application/json;charset=UTF-8',
                    },
                    body: JSON.stringify(key),
                })
                .then(res => {
                    const json = res.json();
                    return json;
                })
                .catch((err) => {
                    return { isOk: false, m: err } as State<T, TKey>;
                })
                .then(res => {
                    return res as State<T, TKey>;
                });
        }
    }
    batch(): BatchBuilder<T, TKey> {
        return new BatchBuilder<T, TKey>(this.baseUri);
    }
    query(): QueryBuilder<T, TKey> {
        return new QueryBuilder<T, TKey>(this.baseUri);
    }
    public static predicateAsString<T>(predicate: (value: T) => any): string {
        const splittedPredicate = predicate.toString().split('.');
        const variableName = splittedPredicate.slice(-(splittedPredicate.length - 1)).join('.');
        return `_rystem.${variableName}`;
    }
}