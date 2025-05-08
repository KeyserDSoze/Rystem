import { ICommand, ICommandPattern } from "./ICommand";
import { IQuery, IQueryPattern } from "./IQuery";

export interface IRepository<T, TKey> extends IQuery<T, TKey>, ICommand<T, TKey> {
    
}

export interface IRepositoryPattern extends IQueryPattern, ICommandPattern {

}