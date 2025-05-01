export type State<T, TKey> = {
    isOk: boolean;
    entity: {
        key: TKey;
        value: T;
    },
    code: number | null,
    message: string | null
}

export type SerializedState<T, TKey> = {
    i: boolean;
    e: {
        k: TKey;
        v: T;
    },
    c: number | null,
    m: string | null
}

export function isState<T, TKey>(obj: any): obj is State<T, TKey> {
    return (
        typeof obj === 'object' &&
        obj !== null &&
        typeof obj.isOk === 'boolean' &&
        typeof obj.entity === 'object' &&
        obj.entity !== null &&
        'key' in obj.entity &&
        'value' in obj.entity &&
        ('code' in obj || obj.code === null) &&
        ('message' in obj || obj.message === null)
    );
}

export function isSerializedState<T, TKey>(obj: any): obj is SerializedState<T, TKey> {
    return (
        typeof obj === 'object' &&
        obj !== null &&
        typeof obj.i === 'boolean' &&
        (
            obj.e === null || (
                typeof obj.e === 'object' &&
                obj.e !== null &&
                'k' in obj.e &&
                'v' in obj.e
            )
        ) &&
        ('c' in obj || obj.c === null) &&
        ('m' in obj || obj.m === null)
    );
}
