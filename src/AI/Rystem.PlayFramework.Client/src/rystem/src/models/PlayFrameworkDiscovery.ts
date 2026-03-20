export type PlayFrameworkToolSourceType = "service" | "client" | "mcp" | "other";

export interface ForcedToolRequest {
    sceneName: string;
    toolName: string;
    sourceType?: PlayFrameworkToolSourceType;
    sourceName?: string;
    memberName?: string;
}

export interface PlayFrameworkToolInfo {
    sceneName: string;
    toolName: string;
    description?: string;
    sourceType: PlayFrameworkToolSourceType;
    sourceName?: string;
    memberName?: string;
    isCommand?: boolean;
    jsonSchema?: string;
}

export interface PlayFrameworkToolSourceInfo {
    name: string;
    sourceType: PlayFrameworkToolSourceType;
    isAvailable?: boolean;
    errorMessage?: string;
    tools?: PlayFrameworkToolInfo[];
}

export interface PlayFrameworkSceneInfo {
    name: string;
    description: string;
    tools?: PlayFrameworkToolInfo[];
}

export interface PlayFrameworkDiscoveryResponse {
    factoryName: string;
    scenes?: PlayFrameworkSceneInfo[];
    services?: PlayFrameworkToolSourceInfo[];
    clients?: PlayFrameworkToolSourceInfo[];
    mcpServers?: PlayFrameworkToolSourceInfo[];
    others?: PlayFrameworkToolInfo[];
}
