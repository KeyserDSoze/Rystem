import { Entity } from "../../models/Entity";
import { Repository } from "../Repository";
import { WhereBuilder } from "./filter/WhereBuilder";
import { FilterOperationAsString } from "./FilterOperationAsString";
import { FilterOperations } from "./FilterOperations";
import { SerializableFilter } from "./SerializableFilter";

export class QueryBuilder<T, TKey> {
    private filters: SerializableFilter;
    private baseUri: string | null;
    constructor(baseUri: string | null) {
        this.filters = {
            o: [] as Array<FilterOperationAsString>
        } as SerializableFilter;
        this.baseUri = baseUri;
    }
    where(): WhereBuilder<T, TKey> {
        return new WhereBuilder<T, TKey>(this);
    }
    filter(predicate: string): QueryBuilder<T, TKey> {
        this.filters.o.push({
            q: FilterOperations.Where,
            v: predicate
        } as FilterOperationAsString);
        return this;
    }
    top(value: number): QueryBuilder<T, TKey> {
        this.filters.o.push({
            q: FilterOperations.Top,
            v: value.toString()
        } as FilterOperationAsString);
        return this;
    }
    skip(value: number): QueryBuilder<T, TKey> {
        this.filters.o.push({
            q: FilterOperations.Skip,
            v: value.toString()
        } as FilterOperationAsString);
        return this;
    }
    orderBy(predicate: (value: T) => any): QueryBuilder<T, TKey> {
        this.filters.o.push({
            q: FilterOperations.OrderBy,
            v: `_rystem => ${Repository.predicateAsString<T>(predicate)}`
        } as FilterOperationAsString);
        return this;
    }
    orderByDescending(predicate: (value: T) => any): QueryBuilder<T, TKey> {
        this.filters.o.push({
            q: FilterOperations.OrderByDescending,
            v: `_rystem => ${Repository.predicateAsString<T>(predicate)}`
        } as FilterOperationAsString);
        return this;
    }
    thenBy(predicate: (value: T) => any): QueryBuilder<T, TKey> {
        this.filters.o.push({
            q: FilterOperations.ThenBy,
            v: `_rystem => ${Repository.predicateAsString<T>(predicate)}`
        } as FilterOperationAsString);
        return this;
    }
    thenByDescending(predicate: (value: T) => any): QueryBuilder<T, TKey> {
        this.filters.o.push({
            q: FilterOperations.ThenByDescending,
            v: `_rystem => ${ Repository.predicateAsString<T>(predicate) }`
        } as FilterOperationAsString);
        return this;
    }
    execute(): Promise<Array<Entity<T, TKey>>> {
        return fetch(`${this.baseUri}/Query`,
            {
                method: 'POST',
                headers: {
                    'content-type': 'application/json;charset=UTF-8',
                },
                body: JSON.stringify(this.filters),
            })
            .then(res => {
                const json = res.json();
                return json;
            })
            .catch((err) => {
                return {} as Array<Entity<T, TKey>>;
            })
            .then(res => {
                return res as Array<Entity<T, TKey>>;
            })
    }
    count(): Promise<number> {
        return this.executeOperation("Count", "long");
    }
    max(predicate: (value: T) => any): Promise<number> {
        this.filters.o.push({
            q: FilterOperations.Select,
            v: `_rystem => ${Repository.predicateAsString<T>(predicate)}`
        } as FilterOperationAsString);
        return this.executeOperation("Max", "decimal");
    }
    min(predicate: (value: T) => any): Promise<number> {
        this.filters.o.push({
            q: FilterOperations.Select,
            v: `_rystem => ${Repository.predicateAsString<T>(predicate)}`
        } as FilterOperationAsString);
        return this.executeOperation("Min", "decimal");
    }
    average(predicate: (value: T) => any): Promise<number> {
        this.filters.o.push({
            q: FilterOperations.Select,
            v: `_rystem => ${Repository.predicateAsString<T>(predicate)}`
        } as FilterOperationAsString);
        return this.executeOperation("Average", "decimal");
    }
    sum(predicate: (value: T) => any): Promise<number> {
        this.filters.o.push({
            q: FilterOperations.Select,
            v: `_rystem => ${Repository.predicateAsString<T>(predicate)}`
        } as FilterOperationAsString);
        return this.executeOperation("Sum", "decimal");
    }
    private executeOperation(operation: string, returnType: string): Promise<number> {
        return fetch(`${this.baseUri}/Operation?op=${operation}&returnType=${returnType}`,
            {
                method: 'POST',
                headers: {
                    'content-type': 'application/json;charset=UTF-8',
                },
                body: JSON.stringify(this.filters),
            })
            .then(res => {
                const json = res.json();
                return json;
            })
            .catch((err) => {
                throw new Error(err);
            })
            .then(res => {
                return res as number;
            })
    }
}