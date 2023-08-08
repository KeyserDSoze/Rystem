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
    select(predicate: (value: T) => any): WhereBuilder<T, TKey> {
        this.queryBuilder.push(Repository.predicateAsString<T>(predicate));
        return this;
    }
    private operator(operation: Operators, value: any | null): WhereBuilder<T, TKey> {
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
    private valueAsString(v: any | null): string {
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
    equal(value: any | null): WhereBuilder<T, TKey> {
        return this
            .operator(Operators.Equal, value);
    }
    notEqual(value: any | null): WhereBuilder<T, TKey> {
        return this
            .operator(Operators.NotEqual, value);
    }
    greaterThan(value: any | null): WhereBuilder<T, TKey> {
        return this
            .operator(Operators.GreaterThan, value);
    }
    greaterThanOrEqual(value: any | null): WhereBuilder<T, TKey> {
        return this
            .operator(Operators.GreaterThanOrEqual, value);
    }
    lesserThan(value: any | null): WhereBuilder<T, TKey> {
        return this
            .operator(Operators.LesserThan, value);
    }
    lesserThanOrEqual(value: any | null): WhereBuilder<T, TKey> {
        return this
            .operator(Operators.LesserThanOrEqual, value);
    }
    startsWith(value: any | null): WhereBuilder<T, TKey> {
        return this
            .operator(Operators.StartsWith, value);
    }
    endsWith(value: any | null): WhereBuilder<T, TKey> {
        return this
            .operator(Operators.EndsWith, value);
    }
    contains(value: any | null): WhereBuilder<T, TKey> {
        return this
            .operator(Operators.Contains, value);
    }
    and(): WhereBuilder<T, TKey> {
        this.queryBuilder.push(" && ");
        return this;
    }
    or(): WhereBuilder<T, TKey> {
        this.queryBuilder.push(" || ");
        return this;
    }
    openRoundBracket(): WhereBuilder<T, TKey> {
        this.queryBuilder.push("(");
        this.roundBracketsCount++;
        return this;
    }
    closeRoundBracket(): WhereBuilder<T, TKey> {
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