"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.useQuery = exports.useCommand = exports.useRepository = void 0;
const RepositoryServices_1 = require("../servicecollection/RepositoryServices");
const useRepository = function (name) {
    return RepositoryServices_1.RepositoryServices.Repository(name);
};
exports.useRepository = useRepository;
const useCommand = function (name) {
    return RepositoryServices_1.RepositoryServices.Command(name);
};
exports.useCommand = useCommand;
const useQuery = function (name) {
    return RepositoryServices_1.RepositoryServices.Repository(name);
};
exports.useQuery = useQuery;
//# sourceMappingURL=hooks.js.map