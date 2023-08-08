export class Container {
    // holding instances of injectable classes by key
    private static registry: Map<string, any> = new Map();

    static register(key: string, instance: any) {
        if (!Container.registry.has(key)) {
            Container.registry.set(key, instance);
            console.log(`Added ${key} to the registry.`);
        }
    }

    static get(key: string) {
        return Container.registry.get(key)
    }
}

// in order to know which parameters of the constructor (index) should be injected (identified by key)
interface Injection {
    index: number;
    key: string;
}

// add to class which has constructor paramteters marked with @inject()
function injectionTarget() {
    return function injectionTarget<T extends { new(...args: any[]): {} }>(constructor: T): T | void {
        // replacing the original constructor with a new one that provides the injections from the Container
        return class extends constructor {
            constructor(...args: any[]) {
                // get injections from class; previously created by @inject()
                const injections = (constructor as any).injections as Injection[]
                // get the instances to inject from the Container
                // this implementation does not support args which should not be injected
                const injectedArgs: any[] = injections.map(({ key }) => {
                    console.log(`Injecting an instance identified by key ${key}`)
                    return Container.get(key)
                })
                // call original constructor with injected arguments
                super(...injectedArgs);
            }
        }
    }
}

// mark constructor parameters which should be injected
// this stores the information about the properties which should be injected
function inject(key: string) {
    return function (target: Object, propertyKey: string | symbol, parameterIndex: number) {
        const injection: Injection = { index: parameterIndex, key }
        const existingInjections: Injection[] = (target as any).injections || []
        // create property 'injections' holding all constructor parameters, which should be injected
        Object.defineProperty(target, "injections", {
            enumerable: false,
            configurable: false,
            writable: false,
            value: [...existingInjections, injection]
        })
    }
}

type User = { name: string; }

// example for a class to be injected
class UserRepository {
    findAllUser(): User[] {
        return [{ name: "Jannik" }, { name: "Max" }]
    }
}

@injectionTarget()
class UserService {
    userRepository: UserRepository;

    // an instance of the UserRepository class, identified by key 'UserRepositroy' should be injected
    constructor(@inject("UserRepository") userRepository?: UserRepository) {
        // ensures userRepository exists and no checks for undefined are required throughout the class
        if (!userRepository) throw Error("No UserRepository provided or injected.")
        this.userRepository = userRepository;
    }

    getAllUser(): User[] {
        // access to an instance of UserRepository
        return this.userRepository.findAllUser()
    }
}

export function Runner2() {
    // initially register all classes which should be injectable with the Container
    Container.register("UserRepository", new UserRepository())

    const userService = new UserService()
    // userService has access to an instance of UserRepository without having it provided in the constructor
    // -> it has been injected!
    console.log(userService.getAllUser())
}