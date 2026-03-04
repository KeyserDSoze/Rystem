using System.Security.Cryptography;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OpenAI.Files;

namespace Rystem.PlayFramework.Adapters;

/// <summary>
/// DelegatingChatClient that uploads non-image binary files via the OpenAI Files API
/// and replaces inline DataContent with HostedFileContent(file_id).
/// Cache strategy (first match wins):
///   1. IDistributedCache (if available)
///   2. IMemoryCache (if available)
///   3. In-memory Dictionary fallback (always available)
/// Also checks the remote Files API by filename to avoid re-uploading.
/// </summary>
public sealed class MultiModalChatClient : DelegatingChatClient
{
    private readonly OpenAIFileClient _fileClient;
    private readonly IDistributedCache? _distributedCache;
    private readonly IMemoryCache? _memoryCache;
    private readonly ILogger<MultiModalChatClient>? _logger;

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(12);
    private const string CachePrefix = "file_upload:";

    /// <summary>Fallback in-memory cache when no IMemoryCache/IDistributedCache is available.</summary>
    private readonly Dictionary<string, string>? _fallbackCache;

    /// <summary>Lazily loaded remote file index: fileName → fileId.</summary>
    private Dictionary<string, string>? _remoteIndex;

    public MultiModalChatClient(
        IChatClient innerClient,
        OpenAIFileClient fileClient,
        IDistributedCache? distributedCache = null,
        IMemoryCache? memoryCache = null,
        ILogger<MultiModalChatClient>? logger = null)
        : base(innerClient)
    {
        _fileClient = fileClient;
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _logger = logger;

        // Only create fallback dictionary if no external cache is available
        if (_distributedCache is null && _memoryCache is null)
        {
            _fallbackCache = new();
            _logger?.LogInformation("FileUploadChatClient: using in-memory Dictionary fallback (no IDistributedCache or IMemoryCache registered)");
        }
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var processed = await UploadFilesInMessagesAsync(messages, cancellationToken);
        return await base.GetResponseAsync(processed, options, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var processed = UploadFilesInMessagesAsync(messages, cancellationToken)
            .GetAwaiter().GetResult();
        return base.GetStreamingResponseAsync(processed, options, cancellationToken);
    }

    private async Task<List<ChatMessage>> UploadFilesInMessagesAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var result = new List<ChatMessage>();
        foreach (var message in messages)
        {
            if (!message.Contents.OfType<DataContent>().Any(IsUploadableFile))
            {
                result.Add(message);
                continue;
            }

            var newContents = new List<AIContent>();
            foreach (var content in message.Contents)
            {
                if (content is DataContent data && IsUploadableFile(data))
                {
                    var hosted = await UploadAndReplaceAsync(data, cancellationToken);
                    newContents.Add(hosted);
                }
                else
                {
                    newContents.Add(content);
                }
            }

            result.Add(new ChatMessage(message.Role, newContents));
        }
        return result;
    }

    private async Task<HostedFileContent> UploadAndReplaceAsync(
        DataContent data,
        CancellationToken cancellationToken)
    {
        var bytes = data.Data is { Length: > 0 } d ? d.ToArray() : [];

        // 1. Resolve file name
        var fileName = data.Name;
        if (string.IsNullOrEmpty(fileName)
            && data.AdditionalProperties?.TryGetValue("name", out var n) == true)
        {
            fileName = n?.ToString();
        }
        fileName ??= $"upload_{Guid.NewGuid():N}";
        if (!Path.HasExtension(fileName) && !string.IsNullOrEmpty(data.MediaType))
        {
            fileName += GetExtensionFromMediaType(data.MediaType);
        }

        // 2. Check cache by content hash
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        var cacheKey = $"{CachePrefix}{hash}";

        var cachedFileId = await GetFromCacheAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrEmpty(cachedFileId))
        {
            _logger?.LogDebug("Cache hit (hash), reusing file_id={FileId} ({FileName})", cachedFileId, fileName);
            return new HostedFileContent(cachedFileId) { Name = fileName };
        }

        // 3. Check remote Files API by filename (lazy-loaded, one-time)
        var remoteFileId = await FindRemoteFileByNameAsync(fileName, cancellationToken);
        if (remoteFileId is not null)
        {
            await SetInCacheAsync(cacheKey, remoteFileId, cancellationToken);
            _logger?.LogDebug("Found on Azure by filename, reusing file_id={FileId} ({FileName})", remoteFileId, fileName);
            return new HostedFileContent(remoteFileId) { Name = fileName };
        }

        // 4. Upload
        using var stream = new MemoryStream(bytes);
        var uploaded = await _fileClient.UploadFileAsync(
            stream, fileName, FileUploadPurpose.Assistants, cancellationToken);

        var fileId = uploaded.Value.Id;
        await SetInCacheAsync(cacheKey, fileId, cancellationToken);
        _remoteIndex?.TryAdd(fileName, fileId);
        _logger?.LogInformation("Uploaded file {FileName} -> file_id={FileId}", fileName, fileId);

        return new HostedFileContent(fileId) { Name = fileName };
    }

    #region Cache helpers (IDistributedCache → IMemoryCache → Dictionary)

    private async Task<string?> GetFromCacheAsync(string key, CancellationToken cancellationToken)
    {
        if (_distributedCache is not null)
        {
            var value = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        if (_memoryCache is not null && _memoryCache.TryGetValue<string>(key, out var memValue) && !string.IsNullOrEmpty(memValue))
            return memValue;

        if (_fallbackCache is not null && _fallbackCache.TryGetValue(key, out var fallbackValue))
            return fallbackValue;

        return null;
    }

    private async Task SetInCacheAsync(string key, string fileId, CancellationToken cancellationToken)
    {
        if (_distributedCache is not null)
        {
            await _distributedCache.SetStringAsync(key, fileId, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration
            }, cancellationToken);
        }

        _memoryCache?.Set(key, fileId, CacheExpiration);
        _fallbackCache?.TryAdd(key, fileId);
    }

    #endregion

    private async Task<string?> FindRemoteFileByNameAsync(string fileName, CancellationToken cancellationToken)
    {
        if (_remoteIndex is null)
        {
            try
            {
                var files = await _fileClient.GetFilesAsync(cancellationToken);
                _remoteIndex = new(StringComparer.OrdinalIgnoreCase);
                foreach (var f in files.Value)
                {
                    _remoteIndex.TryAdd(f.Filename, f.Id);
                }
                _logger?.LogDebug("Loaded {Count} existing files from remote storage", _remoteIndex.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not list remote files, skipping remote check");
                _remoteIndex = new(StringComparer.OrdinalIgnoreCase);
            }
        }

        return _remoteIndex.TryGetValue(fileName, out var id) ? id : null;
    }

    /// <summary>
    /// Returns true for non-image DataContent that should be uploaded via Files API.
    /// Images are handled natively inline by the bridge.
    /// </summary>
    private static bool IsUploadableFile(DataContent data)
    {
        var mediaType = data.MediaType ?? string.Empty;
        return !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    internal static string GetExtensionFromMediaType(string mediaType) => mediaType.ToLowerInvariant() switch
    {
        "application/pdf" => ".pdf",
        "text/plain" => ".txt",
        "text/csv" => ".csv",
        "text/html" => ".html",
        "application/json" => ".json",
        "application/xml" or "text/xml" => ".xml",
        "audio/mpeg" => ".mp3",
        "audio/wav" => ".wav",
        "audio/ogg" => ".ogg",
        "video/mp4" => ".mp4",
        "video/webm" => ".webm",
        "application/zip" => ".zip",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ".pptx",
        _ => string.Empty
    };
}
