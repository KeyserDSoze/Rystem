import { ICommand, ICommandPattern } from "../interfaces/ICommand";
import { IQuery, IQueryPattern } from "../interfaces/IQuery";
import { IRepository, IRepositoryPattern } from "../interfaces/IRepository"
import { RepositoryServices } from "../servicecollection/RepositoryServices"

export const useRepository = function <T, TKey>(name: string): IRepository<T, TKey> {
    return RepositoryServices.Repository<T, TKey>(name);
}
export const useCommand = function <T, TKey>(name: string): ICommand<T, TKey> {
    return RepositoryServices.Command<T, TKey>(name);
}
export const useQuery = function <T, TKey>(name: string): IQuery<T, TKey> {
    return RepositoryServices.Query<T, TKey>(name);
}

export const useRepositoryPattern = function (name: string): IRepositoryPattern {
    return RepositoryServices.RepositoryPattern(name);
}
export const useCommandPattern = function (name: string): ICommandPattern {
    return RepositoryServices.CommandPattern(name);
}
export const useQueryPattern = function (name: string): IQueryPattern {
    return RepositoryServices.QueryPattern(name);
}