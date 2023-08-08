export enum FilterOperations {
    Select = 1,
    Where = 2,
    Top = 4,
    Skip = 8,
    OrderBy = 16,
    OrderByDescending = 32,
    ThenBy = 64,
    ThenByDescending = 128,
    GroupBy = 256
}
