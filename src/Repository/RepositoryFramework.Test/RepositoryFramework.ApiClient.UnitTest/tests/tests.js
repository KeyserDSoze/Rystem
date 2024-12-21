"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const index_1 = require("../../../repositoryframework.api.client.typescript/src/rystem/src/index");
const Setup_1 = require("./setup/Setup");
const assert = require('assert');
describe('Test setup', function () {
    it("Test all names", function () {
        (0, Setup_1.Setup)();
        const arrayOfNames = ["test", "test2"];
        for (let name of arrayOfNames) {
            const repository = index_1.RepositoryServices
                .Repository(name);
            assert.ok(repository != null, "Setup is not working.");
        }
    });
});
//# sourceMappingURL=tests.js.map