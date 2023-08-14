import { CancellationToken } from "typescript";
import { Entity } from "../../../models/Entity";
import { Repository } from "../../Repository";
import { QueryBuilder } from "../QueryBuilder";

export class WhereBuilder<T, TKey> {
    private roundBracketsCount: number;
    private queryBuilder: Array<string>;
    private query: QueryBuilder<T, TKey>;
    constructor(query: QueryBuilder<T, TKey>) {
        this.roundBracketsCount = 0;
        this.queryBuilder = new Array<string>();
        this.queryBuilder.push("_rystem => ");
        this.query = query;
    }
    select(predicate: (value: T) => any): this {
        this.queryBuilder.push(Repository.predicateAsString<T>(predicate));
        return this;
    }
    private operator(operation: Operators, value: any): this {
        switch (operation) {
            case Operators.Equal:
                this.queryBuilder.push(" == ");
                this.queryBuilder.push(this.valueAsString(value));
                break;
            case Operators.NotEqual:
                this.queryBuilder.push(" != ");
                this.queryBuilder.push(this.valueAsString(value));
                break;
            case Operators.GreaterThan:
                this.queryBuilder.push(" > ");
                this.queryBuilder.push(this.valueAsString(value));
                break;
            case Operators.GreaterThanOrEqual:
                this.queryBuilder.push(" >= ");
                this.queryBuilder.push(this.valueAsString(value));
                break;
            case Operators.LesserThan:
                this.queryBuilder.push(" < ");
                this.queryBuilder.push(this.valueAsString(value));
                break;
            case Operators.LesserThanOrEqual:
                this.queryBuilder.push(" <= ");
                this.queryBuilder.push(this.valueAsString(value));
                break;
            case Operators.Contains:
                this.queryBuilder.push(".Contains(");
                this.queryBuilder.push(this.valueAsString(value));
                this.queryBuilder.push(")");
                break;
            case Operators.StartsWith:
                this.queryBuilder.push(".StartsWith(");
                this.queryBuilder.push(this.valueAsString(value));
                this.queryBuilder.push(")");
                break;
            case Operators.EndsWith:
                this.queryBuilder.push(".EndsWith(");
                this.queryBuilder.push(this.valueAsString(value));
                this.queryBuilder.push(")");
                break;
        }
        return this;
    }
    private valueAsString(v: any): string {
        if (v != null) {
            if (typeof v == 'number') {
                return v.toString();
            } else {
                return `"${v.toString()}"`;
            }
        }
        else
            return "null";
    }
    equal(value: any): this {
        return this
            .operator(Operators.Equal, value);
    }
    notEqual(value: any): this {
        return this
            .operator(Operators.NotEqual, value);
    }
    greaterThan(value: any): this {
        return this
            .operator(Operators.GreaterThan, value);
    }
    greaterThanOrEqual(value: any): this {
        return this
            .operator(Operators.GreaterThanOrEqual, value);
    }
    lesserThan(value: any): this {
        return this
            .operator(Operators.LesserThan, value);
    }
    lesserThanOrEqual(value: any): this {
        return this
            .operator(Operators.LesserThanOrEqual, value);
    }
    startsWith(value: any): this {
        return this
            .operator(Operators.StartsWith, value);
    }
    endsWith(value: any): this {
        return this
            .operator(Operators.EndsWith, value);
    }
    contains(value: any): this {
        return this
            .operator(Operators.Contains, value);
    }
    and(): this {
        this.queryBuilder.push(" && ");
        return this;
    }
    or(): this {
        this.queryBuilder.push(" || ");
        return this;
    }
    openRoundBracket(): this {
        this.queryBuilder.push("(");
        this.roundBracketsCount++;
        return this;
    }
    closeRoundBracket(): this {
        this.queryBuilder.push(")");
        this.roundBracketsCount--;
        return this;
    }
    build(): QueryBuilder<T, TKey> {
        for (let i = 0; i < this.roundBracketsCount; i++) {
            this.queryBuilder.push(")");
        }
        this.roundBracketsCount = 0;
        return this.query.filter(this.queryBuilder.join(''));
    }
    execute(): Promise<Array<Entity<T, TKey>>> {
        return this.build().execute();
    }
    executeAsStream(entityReader: (entity: Entity<T, TKey>) => void,
        cancellationToken: CancellationToken | null = null): Promise<Array<Entity<T, TKey>>> {
        return this.build().executeAsStream(entityReader, cancellationToken);
    }
    count(): Promise<number> {
        return this.build().count();
    }
    max(predicate: (value: T) => any): Promise<number> {
        return this.build().max(predicate);
    }
    min(predicate: (value: T) => any): Promise<number> {
        return this.build().min(predicate);
    }
    average(predicate: (value: T) => any): Promise<number> {
        return this.build().average(predicate);
    }
    sum(predicate: (value: T) => any): Promise<number> {
        return this.build().sum(predicate);
    }
}

enum Operators {
    Equal = 1,
    NotEqual = 2,
    GreaterThan = 3,
    GreaterThanOrEqual = 4,
    LesserThan = 5,
    LesserThanOrEqual = 6,
    Contains = 7,
    StartsWith = 8,
    EndsWith = 9
}