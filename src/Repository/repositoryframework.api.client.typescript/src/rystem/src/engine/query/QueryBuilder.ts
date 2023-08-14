import { CancellationToken } from "typescript";
import { Entity } from "../../models/Entity";
import { RepositoryEndpoint } from "../../models/RepositoryEndpoint";
import { Repository } from "../Repository";
import { WhereBuilder } from "./filter/WhereBuilder";
import { FilterOperationAsString } from "./FilterOperationAsString";
import { FilterOperations } from "./FilterOperations";
import { SerializableFilter } from "./SerializableFilter";

export class QueryBuilder<T, TKey> {
    private filters: SerializableFilter;
    private repository: Repository<T, TKey>;
    constructor(repository: Repository<T, TKey>) {
        this.filters = {
            o: [] as Array<FilterOperationAsString>
        } as SerializableFilter;
        this.repository = repository;
    }
    where(): WhereBuilder<T, TKey> {
        return new WhereBuilder<T, TKey>(this);
    }
    filter(predicate: string): this {
        this.filters.o.push({
            q: FilterOperations.Where,
            v: predicate
        } as FilterOperationAsString);
        return this;
    }
    top(value: number): this {
        this.filters.o.push({
            q: FilterOperations.Top,
            v: value.toString()
        } as FilterOperationAsString);
        return this;
    }
    skip(value: number): this {
        this.filters.o.push({
            q: FilterOperations.Skip,
            v: value.toString()
        } as FilterOperationAsString);
        return this;
    }
    orderBy(predicate: (value: T) => any): this {
        this.filters.o.push({
            q: FilterOperations.OrderBy,
            v: `_rystem => ${Repository.predicateAsString<T>(predicate)}`
        } as FilterOperationAsString);
        return this;
    }
    orderByDescending(predicate: (value: T) => any): this {
        this.filters.o.push({
            q: FilterOperations.OrderByDescending,
            v: `_rystem => ${Repository.predicateAsString<T>(predicate)}`
        } as FilterOperationAsString);
        return this;
    }
    thenBy(predicate: (value: T) => any): this {
        this.filters.o.push({
            q: FilterOperations.ThenBy,
            v: `_rystem => ${Repository.predicateAsString<T>(predicate)}`
        } as FilterOperationAsString);
        return this;
    }
    thenByDescending(predicate: (value: T) => any): this {
        this.filters.o.push({
            q: FilterOperations.ThenByDescending,
            v: `_rystem => ${Repository.predicateAsString<T>(predicate)}`
        } as FilterOperationAsString);
        return this;
    }
    execute(): Promise<Array<Entity<T, TKey>>> {
        return this.repository.makeRequest<Array<Entity<T, TKey>>>(
            RepositoryEndpoint.Query,
            `Query`, 'POST',
            this.filters,
            {
                'content-type': 'application/json;charset=UTF-8',
            });
    }
    executeAsStream(entityReader: (entity: Entity<T, TKey>) => void,
        cancellationToken: CancellationToken | null = null): Promise<Array<Entity<T, TKey>>> {
        return this.repository.makeRequestAsStream<Entity<T, TKey>>(
            RepositoryEndpoint.QueryStream,
            `Query`, 'POST',
            entityReader,
            this.filters,
            {
                'content-type': 'application/json;charset=UTF-8',
            }, cancellationToken);
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
        return this.repository.makeRequest<number>(RepositoryEndpoint.Operation,
            `Operation?op=${operation}&returnType=${returnType}`, 'POST',
            this.filters,
            {
                'content-type': 'application/json;charset=UTF-8',
            });
    }
}