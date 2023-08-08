import { IperUser } from "../Models/IperUser";
import { SuperUser } from "../Models/SuperUser";
import { BatchResults, IRepository, RepositoryServices, State } from "../rystem";

export function Setup() {
    RepositoryServices
        .Create("https://localhost:7058/api/")
        .addRepository<IperUser, string>(x => {
            x.name = "test";
            x.path = "SuperUser";
        })
        .addRepository<SuperUser, string>(x => {
            x.name = "test2"
            x.path = "SuperUser/inmemory";
        });
}

function repositoryInject(factoryName: string) {
    return function (classInstance: any, propertyName: string) {
        Object.defineProperty(classInstance, propertyName, {
            get: () => RepositoryServices.Repository<IperUser, string>(factoryName),
            set: (c) => c,
            enumerable: true,
            configurable: true,
        });
    };
}

class Example {
    @repositoryInject("test")
    public repository: IRepository<IperUser, string> | undefined;

    something(): IRepository<IperUser, string> | undefined {
        this.repository?.batch();
        return this.repository;
    }
}

export async function Runner(repository: IRepository<IperUser, string> | undefined) {
    //const repository = RepositoryServices
    //    .Repository<IperUser, string>("test");
    const example = new Example();
    repository = example.something();
    if (repository != undefined) {
        const x = Math.floor(Math.random() * (30000 - 0 + 1)) + 0;
        const id = `${x}_Key_4942b090-f6a0-45a4-a188-286807f6bb9c`;
        const iperUser = {
            id: id,
            name: "corazon",
            email: "calcutta@gmail.com",
            port: 324324,
            isAdmin: true,
            groupId: "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        } as IperUser;
        let response2: State<IperUser, string> = await repository.insert(id, iperUser);
        console.log("insert: " + response2.isOk);
        response2 = await repository.exist(id);
        console.log("exist: " + response2.isOk);
        response2 = await repository.delete(id);
        console.log("delete: " + response2.isOk);
        response2 = await repository.exist(id);
        console.log("exist after delete: " + response2.isOk);
        response2 = await repository.insert(id, iperUser);
        console.log("insert after delete: " + response2.isOk);
        response2 = await repository.update(id, iperUser);
        console.log("update after delete: " + response2.isOk);
        response2 = await repository.exist(id);
        console.log("exist after update: " + response2.isOk);
        const batcher = repository.batch();
        for (let i = 0; i < 10; i++) {
            const x1 = Math.floor(Math.random() * (30000 - 0 + 1)) + 0;
            const id1 = `${x1}_Key_4942b090-f6a0-45a4-a188-286807f6bb9c`;
            const iperUser1 = {
                id: id1,
                name: "corazon1",
                email: "calcutt1a@gmail.com",
                port: 3243241,
                isAdmin: false,
                groupId: "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            } as IperUser;
            batcher
                .addInsert(id1, iperUser1)
                .addUpdate(id1, iperUser1)
                .addDelete(id1);
        }
        const batchResults: BatchResults<IperUser, string> = await batcher.execute();
        console.log(batchResults);
        let queryResults = await repository.query().execute();
        console.log(queryResults);
        queryResults = await repository.query().filter(`x => x.id == "${id}"`).execute();
        console.log(queryResults);
        queryResults = await repository.query()
            .where()
            .openRoundBracket()
            .select(x => x.id)
            .equal(id)
            .build()
            .orderBy(x => x.name)
            .execute();
        console.log(queryResults);
        const count = await repository
            .query()
            .where()
            .openRoundBracket()
            .select(x => x.id)
            .equal(id)
            .build()
            .count();
        console.log(count);
        const sum = await repository
            .query()
            .where()
            .openRoundBracket()
            .select(x => x.id)
            .equal(id)
            .build()
            .sum(x => x.port);
        console.log(sum);
        const portGreaterThanZero = await repository
            .query()
            .where()
            .openRoundBracket()
            .select(x => x.port)
            .greaterThanOrEqual(0)
            .build()
            .count();
        console.log(portGreaterThanZero);
        const portGreaterThanZeroOrderedByName = await repository
            .query()
            .where()
            .openRoundBracket()
            .select(x => x.port)
            .greaterThanOrEqual(0)
            .build()
            .orderBy(x => x.name)
            .execute();
        console.log(portGreaterThanZeroOrderedByName);
    }
}