import { RepositoryEndpoint } from "../models/RepositoryEndpoint";

export class RepositorySettings {
    name: string;
    uri: string | null;
    path: string | null;
    complexKey: boolean;
    private headersEnrichers: Array<(endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any) => HeadersInit>;
    private errorsHandlers: Array<(endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any, err: any) => boolean >;

    constructor() {
        this.name = "";
        this.uri = null;
        this.path = null;
        this.complexKey = false;
        this.headersEnrichers = new Array<(endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any) => HeadersInit>();
        this.errorsHandlers = new Array<(endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any, err: any) => boolean>();
    }

    public addHeadersEnricher(enricher: (endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any) => HeadersInit): this {
        this.headersEnrichers.push(enricher);
        return this;
    }
    public addErrorHandler(handler: (endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any, err: any) => boolean): this {
        this.errorsHandlers.push(handler);
        return this;
    }
    public enrichHeaders(endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit | undefined, body: any): HeadersInit {
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
            setHeaders(enricher(endpoint, uri, method, requestHeaders, body));
        }
        return requestHeaders;
    }
    public manageError(endpoint: RepositoryEndpoint, uri: string, method: string, headers: HeadersInit, body: any, err: any): boolean {
        let retry: boolean = this.errorsHandlers.length > 0;
        for (let handler of this.errorsHandlers)
            retry &&= handler(endpoint, uri, method, headers, body, err);
        return retry;
    }
}