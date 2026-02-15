/**
 * Configuration for PlayFramework client.
 */
export class PlayFrameworkSettings {
    /**
     * Factory name (for multi-factory endpoints).
     */
    factoryName: string;

    /**
     * Base API URL (e.g., "https://api.example.com/api/ai").
     */
    baseUrl: string;

    /**
     * Default headers to include in all requests.
     */
    defaultHeaders: HeadersInit;

    /**
     * Request timeout in milliseconds (default: 60000ms = 60s).
     */
    timeout: number;

    /**
     * Header enrichers (e.g., add Authorization header dynamically).
     */
    private headersEnrichers: Array<(url: string, method: string, headers: HeadersInit, body: any) => Promise<HeadersInit>>;

    /**
     * Error handlers for retry logic.
     */
    private errorHandlers: Array<(url: string, method: string, headers: HeadersInit, body: any, error: any) => Promise<boolean>>;

    constructor(factoryName: string, baseUrl: string) {
        this.factoryName = factoryName;
        this.baseUrl = baseUrl;
        this.defaultHeaders = {
            "Content-Type": "application/json"
        };
        this.timeout = 60000; // 60 seconds
        this.headersEnrichers = [];
        this.errorHandlers = [];
    }

    /**
     * Add a header enricher function (e.g., dynamic Authorization token).
     */
    public addHeadersEnricher(enricher: (url: string, method: string, headers: HeadersInit, body: any) => Promise<HeadersInit>): this {
        this.headersEnrichers.push(enricher);
        return this;
    }

    /**
     * Add an error handler for retry logic.
     */
    public addErrorHandler(handler: (url: string, method: string, headers: HeadersInit, body: any, error: any) => Promise<boolean>): this {
        this.errorHandlers.push(handler);
        return this;
    }

    /**
     * Enrich headers with all registered enrichers.
     */
    public async enrichHeaders(url: string, method: string, headers: HeadersInit | undefined, body: any): Promise<HeadersInit> {
        const requestHeaders: Headers = new Headers();

        const setHeaders = (currentHeaders: HeadersInit) => {
            if (currentHeaders instanceof Headers) {
                currentHeaders.forEach((value, key) => {
                    if (!requestHeaders.has(key)) {
                        requestHeaders.set(key, value);
                    }
                });
            } else if (Array.isArray(currentHeaders)) {
                currentHeaders.forEach(([key, value]) => {
                    if (!requestHeaders.has(key)) {
                        requestHeaders.set(key, value);
                    }
                });
            } else {
                for (const [key, value] of Object.entries(currentHeaders)) {
                    if (!requestHeaders.has(key)) {
                        requestHeaders.set(key, value);
                    }
                }
            }
        };

        // Default headers
        setHeaders(this.defaultHeaders);

        // Provided headers
        if (headers) {
            setHeaders(headers);
        }

        // Enrichers
        for (const enricher of this.headersEnrichers) {
            setHeaders(await enricher(url, method, requestHeaders, body));
        }

        return requestHeaders;
    }

    /**
     * Manage errors with all registered handlers. Returns true if should retry.
     */
    public async manageError(url: string, method: string, headers: HeadersInit, body: any, error: any): Promise<boolean> {
        if (this.errorHandlers.length === 0) {
            return false;
        }

        let retry = true;
        for (const handler of this.errorHandlers) {
            retry &&= await handler(url, method, headers, body, error);
        }
        return retry;
    }
}
