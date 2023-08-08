export class RepositorySettings {
    name: string;
    uri: string | null;
    path: string | null;
    complexKey: boolean;

    constructor() {
        this.name = "";
        this.uri = null;
        this.path = null;
        this.complexKey = false;
    }
}