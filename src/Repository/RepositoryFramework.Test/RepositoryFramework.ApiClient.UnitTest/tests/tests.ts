import { RepositoryServices } from "../../../repositoryframework.api.client.typescript/src/rystem/src/index";
import { IperUser } from "./models/IperUser";
import { Setup } from "./setup/Setup";
const assert = require('assert');


describe('Test setup', function () {
    it("Test all names", function () {
        Setup();
        const arrayOfNames = ["test", "test2"];
        for (let name of arrayOfNames) {
            const repository = RepositoryServices
                .Repository<IperUser, string>(name);
            assert.ok(repository != null, "Setup is not working.");
        }
    })
})