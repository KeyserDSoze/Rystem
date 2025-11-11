"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.isBatchOperation = isBatchOperation;
exports.isSerializedBatchOperation = isSerializedBatchOperation;
function isBatchOperation(obj) {
    return (typeof obj === 'object' &&
        obj !== null &&
        (obj.command === 1 || obj.command === 2 || obj.command === 4) &&
        'key' in obj);
}
function isSerializedBatchOperation(obj) {
    return (typeof obj === 'object' &&
        obj !== null &&
        (obj.c === 1 || obj.c === 2 || obj.c === 4) &&
        'k' in obj);
}
//# sourceMappingURL=BatchOperation.js.map