import { Repository } from "../engine/Repository";
import { ICommand } from "../interfaces/ICommand";
import { IQuery } from "../interfaces/IQuery";
import { IRepository } from "../interfaces/IRepository";
import { RepositorySettings } from "./RepositorySettings";

export class RepositoryServices {
    private static instance: RepositoryServices;
    private repositories: Map<string, RepositoryWrapper>;
    private baseUri: string | null;

    private constructor() {
        this.repositories = new Map<string, RepositoryWrapper>();
        this.baseUri = null;
    }
    public static Repository<T, TKey>(name: string): IRepository<T, TKey> {
        return RepositoryServices.Instance(name, RepositoryType.Repository) as IRepository<T, TKey>;
    }
    public static Command<T, TKey>(name: string): ICommand<T, TKey> {
        return RepositoryServices.Instance(name, RepositoryType.Command) as ICommand<T, TKey>;
    }
    public static Query<T, TKey>(name: string): IQuery<T, TKey> {
        return RepositoryServices.Instance(name, RepositoryType.Query) as IQuery<T, TKey>;
    }
    private static Instance<T, TKey>(name: string, type: RepositoryType): IRepository<T, TKey> | ICommand<T, TKey> | IQuery<T, TKey> {
        if (!RepositoryServices.instance) {
            RepositoryServices.instance = new RepositoryServices();
        }
        if (RepositoryServices.instance.repositories.has(name)) {
            const wrapper = RepositoryServices.instance.repositories.get(name);
            if (wrapper != undefined && wrapper.type == type) {
                if (wrapper.type == RepositoryType.Repository)
                    return wrapper.repository as IRepository<T, TKey>;
                else if (wrapper.type == RepositoryType.Command)
                    return wrapper.repository as ICommand<T, TKey>;
                else
                    return wrapper.repository as IQuery<T, TKey>;
            }
        }

        throw new Error(`${type} with '${name}' has a wrong setup.`);
    }

    public static Create<T, TKey>(baseUri: string | null): RepositoryServices {
        if (!RepositoryServices.instance) {
            RepositoryServices.instance = new RepositoryServices();
        }
        RepositoryServices.instance.baseUri = baseUri;
        return RepositoryServices.instance;
    }
    private AddRepository<T, TKey>(builder: (x: RepositorySettings) => void, type: RepositoryType): this {
        const settings = new RepositorySettings();
        builder(settings);
        if (settings.name == null || settings.name == "")
            throw new Error("Repository needs a name during setup. Please provide in settings a name length greater than or equal to 1.");
        const repository = new Repository<T, TKey>(this.baseUri, settings);
        this.repositories.set(settings.name, new RepositoryWrapper(repository, type));
        return this;
    }
    public addRepository<T, TKey>(builder: (x: RepositorySettings) => void): this {
        return this.AddRepository(builder, RepositoryType.Repository);
    }
    public addCommand<T, TKey>(builder: (x: RepositorySettings) => void): this {
        return this.AddRepository(builder, RepositoryType.Command);
    }
    public addQuery<T, TKey>(builder: (x: RepositorySettings) => void): this {
        return this.AddRepository(builder, RepositoryType.Query);
    }
}

enum RepositoryType {
    Repository = 1,
    Command = 2,
    Query = 3
}

class RepositoryWrapper {
    repository: any;
    type: RepositoryType;
    constructor(repository: any, type: RepositoryType) {
        this.repository = repository;
        this.type = type;
    }
}