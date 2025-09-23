import { RepositoryEndpoint } from "../models/RepositoryEndpoint";
export interface ITransformer<T> {
    toPlain: (input: T | any) => any;
    fromPlain: (input: any) => T;
}
export class RepositorySettings {
    name: string;
    uri: string | null;
    path: string | null;
    case: "PascalCase" | "CamelCase";
    transformer?: ITransformer<any>; // For T
    keyTransformer?: ITransformer<any>; // For TKey
    complexKey: boolean;
    private headersEnrichers: Array<(endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any) => Promise<HeadersInit>>;
    private errorsHandlers: Array<(endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any, err: any) => Promise<boolean>>;

    constructor() {
        this.name = "";
        this.uri = null;
        this.path = null;
        this.case = "PascalCase";
        this.complexKey = false;
        this.headersEnrichers = new Array<(endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any) => Promise<HeadersInit>>();
        this.errorsHandlers = new Array<(endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any, err: any) => Promise<boolean>>();
    }

    public addHeadersEnricher(enricher: (endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any) => Promise<HeadersInit>): this {
        this.headersEnrichers.push(enricher);
        return this;
    }
    public addErrorHandler(handler: (endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any, err: any) => Promise<boolean>): this {
        this.errorsHandlers.push(handler);
        return this;
    }
    public async enrichHeaders(endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit | undefined, body: any): Promise<HeadersInit> {
        const requestHeaders: HeadersInit = new Headers();

        const setHeaders = (currentHeaders: HeadersInit) => {
            let forcedHeaders = currentHeaders as Record<string, string>;
            for (let header in currentHeaders) {
                if (!requestHeaders.has(header)) {
                    requestHeaders.set(header, forcedHeaders[header]!);
                }
            }
        };
        if (headers != undefined) {
            setHeaders(headers);
        }
        
        for (let enricher of this.headersEnrichers) {
            setHeaders(await enricher(endpoint, uri, method, requestHeaders, body));
        }
        return requestHeaders;
    }
    public async manageError(endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any, err: any): Promise<boolean> {
        let retry: boolean = this.errorsHandlers.length > 0;
        for (let handler of this.errorsHandlers)
            retry &&= await handler(endpoint, uri, method, headers, body, err);
        return retry;
    }
}