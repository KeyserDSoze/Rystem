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
     * Maximum number of SSE reconnection attempts on network failure (default: 3).
     * Set to 0 to disable automatic reconnection.
     */
    maxReconnectAttempts: number;

    /**
     * Base delay in milliseconds between reconnection attempts (default: 1000ms).
     * Uses exponential backoff: delay * 2^attempt.
     */
    reconnectBaseDelay: number;

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
        this.maxReconnectAttempts = 3;
        this.reconnectBaseDelay = 1000;
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
                    requestHeaders.set(key, value);
                });
            } else if (Array.isArray(currentHeaders)) {
                currentHeaders.forEach(([key, value]) => {
                    requestHeaders.set(key, value);
                });
            } else {
                for (const [key, value] of Object.entries(currentHeaders)) {
                    requestHeaders.set(key, value);
                }
            }
        };

        // Default headers (lowest priority)
        setHeaders(this.defaultHeaders);

        // Provided headers (override defaults)
        if (headers) {
            setHeaders(headers);
        }

        // Enrichers (highest priority)
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
