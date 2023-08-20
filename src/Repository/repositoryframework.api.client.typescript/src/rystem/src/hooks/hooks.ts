import { ICommand } from "../interfaces/ICommand";
import { IQuery } from "../interfaces/IQuery";
import { IRepository } from "../interfaces/IRepository"
import { RepositoryServices } from "../servicecollection/RepositoryServices"

export const useRepository = function <T, TKey>(name: string): IRepository<T, TKey> {
    return RepositoryServices.Repository<T, TKey>(name);
}
export const useCommand = function <T, TKey>(name: string): ICommand<T, TKey> {
    return RepositoryServices.Command<T, TKey>(name);
}
export const useQuery = function <T, TKey>(name: string): IQuery<T, TKey> {
    return RepositoryServices.Repository<T, TKey>(name);
}