"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Setup = void 0;
const index_1 = require("../../../../repositoryframework.api.client.typescript/src/rystem/src/index");
function Setup() {
    index_1.RepositoryServices
        .Create("http://localhost:5000/api/")
        .addRepository(x => {
        x.name = "test";
        x.path = "SuperUser";
        x.addHeadersEnricher((...args) => {
            return {
                "Authorization-UI": "Bearer dsjadjalsdjalsdjalsda"
            };
        });
        x.addErrorHandler((endpoint, uri, method, headers, body, err) => {
            return err.startsWith("big error");
        });
    })
        .addRepository(x => {
        x.name = "test2";
        x.path = "SuperUser/inmemory";
    });
}
exports.Setup = Setup;
//# sourceMappingURL=Setup.js.map