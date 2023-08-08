export class RepositorySettings {
    name: string;
    uri: string | null;
    path: string | null;
    complexKey: boolean;
    private headersEnrichers: Array<(headers: HeadersInit) => HeadersInit>;
    private errorsHandlers: Array<(err: any) => boolean>;

    constructor() {
        this.name = "";
        this.uri = null;
        this.path = null;
        this.complexKey = false;
        this.headersEnrichers = new Array<(headers: HeadersInit) => HeadersInit>();
        this.errorsHandlers = new Array<(err: any) => boolean>();
    }

    public addHeadersEnricher(enricher: (headers: HeadersInit) => HeadersInit): this {
        this.headersEnrichers.push(enricher);
        return this;
    }
    public addErrorHandler(handler: (err: any) => boolean): this {
        this.errorsHandlers.push(handler);
        return this;
    }
    public enrichHeaders(headers: HeadersInit | undefined): HeadersInit {
        if (headers == undefined)
            headers = {} as HeadersInit;
        for (let enricher of this.headersEnrichers)
            headers = enricher(headers);
        return headers;
    }
    public manageError(err: any): boolean {
        let retry: boolean = this.errorsHandlers.length > 0;
        for (let handler of this.errorsHandlers)
            retry &&= handler(err);
        return retry;
    }
}