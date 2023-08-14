import { IperUser } from "../Models/IperUser";
import { SuperUser } from "../Models/SuperUser";
import { RepositoryEndpoint, RepositoryServices } from "../rystem/src";


export function Setup() {
    RepositoryServices
        //.Create("https://localhost:7058/api/")
        .Create("http://localhost:5000/api/")
        .addRepository<IperUser, string>(x => {
            x.name = "test";
            x.path = "SuperUser";
            x.addHeadersEnricher((...args) => {
                return {
                    "Authorization-UI": "Bearer dsjadjalsdjalsdjalsda"
                };
            });
            x.addErrorHandler((endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any, err: any) => {
                return (err as string).startsWith("big error");
            });
        })
        .addRepository<SuperUser, string>(x => {
            x.name = "test2";
            x.path = "SuperUser/inmemory";
        });
}
