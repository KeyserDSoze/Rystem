"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Setup = Setup;
const index_1 = require("../../../../repositoryframework.api.client.typescript/src/rystem/src/index");
function Setup() {
    index_1.RepositoryServices
        .Create("http://localhost:5000/api/")
        .addRepository(x => {
        x.name = "test";
        x.path = "SuperUser";
        x.addHeadersEnricher((...args) => __awaiter(this, void 0, void 0, function* () {
            return {
                "Authorization-UI": "Bearer dsjadjalsdjalsdjalsda"
            };
        }));
        x.addErrorHandler((endpoint, uri, method, headers, body, err) => __awaiter(this, void 0, void 0, function* () {
            return err.startsWith("big error");
        }));
    })
        .addRepository(x => {
        x.name = "test2";
        x.path = "SuperUser/inmemory";
    });
}
//# sourceMappingURL=Setup.js.map