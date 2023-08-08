import { ICommand } from "./ICommand";
import { IQuery } from "./IQuery";

export interface IRepository<T, TKey> extends IQuery<T, TKey>, ICommand<T, TKey> {
    
}