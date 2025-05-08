import { IperUser } from "../Models/IperUser";
import { BatchResult, State, useRepository, useRepositoryPattern } from "../rystem/src";

export async function Runner() {
    const repository = useRepository<IperUser, string>("test");
    const x = Math.floor(Math.random() * (30000 - 0 + 1)) + 0;
    const id = `${x}_Key_4942b090-f6a0-45a4-a188-286807f6bb9c`;
    const iperUser = {
        identifier: id,
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
            identifier: id1,
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
    let batchResults: Array<BatchResult<IperUser, string>> = await batcher.execute();
    console.log(batchResults);
    batchResults = await batcher.executeAsStream(x => console.log(x));
    console.log(batchResults);
    let queryResults = await repository.query().executeAsStream(x => console.log(x));
    console.log(queryResults);
    queryResults = await repository.query().filter(`x => x.Id == "${id}"`).execute();
    console.log(queryResults);
    queryResults = await repository.query()
        .where()
        .openRoundBracket()
        .select(x => x.identifier)
        .equal(id)
        .build()
        .orderBy(x => x.name)
        .execute();
    console.log(queryResults);
    const count = await repository
        .query()
        .where()
        .openRoundBracket()
        .select(x => x.identifier)
        .equal(id)
        .count();
    console.log(count);
    const sum = await repository
        .query()
        .where()
        .openRoundBracket()
        .select(x => x.identifier)
        .equal(id)
        .sum(x => x.port);
    console.log(sum);
    const portGreaterThanZero = await repository
        .query()
        .where()
        .openRoundBracket()
        .select(x => x.port)
        .greaterThanOrEqual(0)
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

export async function RunnerWithAny() {
    try {
        const repository = useRepositoryPattern("test");
        const x = Math.floor(Math.random() * (30000 - 0 + 1)) + 0;
        const id = `${x}_Key_4942b090-f6a0-45a4-a188-286807f6bb9c`;
        const iperUser = {
            identifier: id,
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
                identifier: id1,
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
        let batchResults: Array<BatchResult<IperUser, string>> = await batcher.execute();
        console.log(batchResults);
        batchResults = await batcher.executeAsStream(x => console.log(x));
        console.log(batchResults);
        let queryResults = await repository.query().executeAsStream(x => console.log(x));
        console.log(queryResults);
        queryResults = await repository.query().filter(`x => x.Id == "${id}"`).execute();
        console.log(queryResults);
        queryResults = await repository.query()
            .where()
            .openRoundBracket()
            .select(x => x.identifier)
            .equal(id)
            .build()
            .orderBy(x => x.name)
            .execute();
        console.log(queryResults);
        const count = await repository
            .query()
            .where()
            .openRoundBracket()
            .select(x => x.identifier)
            .equal(id)
            .count();
        console.log(count);
        const sum = await repository
            .query()
            .where()
            .openRoundBracket()
            .select(x => x.identifier)
            .equal(id)
            .sum(x => x.port);
        console.log(sum);
        const portGreaterThanZero = await repository
            .query()
            .where()
            .openRoundBracket()
            .select(x => x.port)
            .greaterThanOrEqual(0)
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
    } catch (exception) {
        console.log(exception);
    }
}