import { plainToInstance, instanceToPlain } from "class-transformer";
import { IperUser } from "../Models/IperUser";
import { SuperUser } from "../Models/SuperUser";
import { RepositoryEndpoint, RepositoryServices, ITransformer } from "../rystem/src";

const IperUserTransformer: ITransformer<IperUser> = {
    fromPlain: (plain: any) => plainToInstance(IperUser, plain),
    toPlain: (instance: IperUser) => instanceToPlain(instance)
};
const IperUserTransformer2: ITransformer<IperUser> = {
    fromPlain: (plain: any): IperUser => {
        return {
            identifier: plain.id,
            name: plain.name,
            groupId: plain.groupId,
            port: plain.port
        };
    },
    toPlain: (instance: IperUser): any => {
        return {
            Id: instance.identifier,
            Name: instance.name,
            GroupId: instance.groupId,
            Port: instance.port
        };
    }
};
export function Setup() {
    RepositoryServices
        .Create("https://localhost:7058/api/")
        //.Create("http://localhost:5000/api/")
        .addRepository<IperUser, string>(x => {
            x.name = "test";
            x.path = "SuperUser";
            x.case = "PascalCase";
            x.transformer = IperUserTransformer2;
            x.addHeadersEnricher(async (...args) => {
                return {
                    "Authorization-UI": "Bearer dsjadjalsdjalsdjalsda"
                };
            });
            x.addErrorHandler(async (endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any, err: any) => {
                return err.Message ?? "error";
            });
        })
        .addRepository<SuperUser, string>(x => {
            x.name = "test2";
            x.path = "SuperUser/inmemory";
        });
}
