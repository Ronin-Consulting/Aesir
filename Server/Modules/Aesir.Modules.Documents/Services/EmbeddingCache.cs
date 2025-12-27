using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.AI;

namespace Aesir.Modules.Documents.Services;

/// <summary>
/// Provides a thread-safe cache for text embeddings using content-based hashing.
/// </summary>
public interface IEmbeddingCache
{
    /// <summary>
    /// Gets a cached embedding for the given text, or null if not found.
    /// </summary>
    /// <param name="text">The text to look up.</param>
    /// <returns>The cached embedding, or null if not found.</returns>
    Embedding<float>? Get(string text);

    /// <summary>
    /// Stores an embedding in the cache for the given text.
    /// </summary>
    /// <param name="text">The text that was embedded.</param>
    /// <param name="embedding">The embedding to cache.</param>
    void Set(string text, Embedding<float> embedding);

    /// <summary>
    /// Clears all cached embeddings.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the current number of cached embeddings.
    /// </summary>
    int Count { get; }
}

/// <summary>
/// In-memory embedding cache with SHA256-based content hashing and simple LRU eviction.
/// </summary>
public class EmbeddingCache : IEmbeddingCache
{
    private readonly ConcurrentDictionary<string, Embedding<float>> _cache = new();
    private readonly int _maxEntries;

    /// <summary>
    /// Initializes a new instance of the EmbeddingCache.
    /// </summary>
    /// <param name="maxEntries">Maximum number of entries before eviction occurs.</param>
    public EmbeddingCache(int maxEntries = 10000)
    {
        _maxEntries = maxEntries;
    }

    /// <inheritdoc />
    public int Count => _cache.Count;

    /// <inheritdoc />
    public Embedding<float>? Get(string text)
    {
        var hash = ComputeHash(text);
        return _cache.TryGetValue(hash, out var embedding) ? embedding : null;
    }

    /// <inheritdoc />
    public void Set(string text, Embedding<float> embedding)
    {
        var hash = ComputeHash(text);

        // Simple eviction: clear half when at capacity
        if (_cache.Count >= _maxEntries)
        {
            var keysToRemove = _cache.Keys.Take(_maxEntries / 2).ToList();
            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
        }

        _cache[hash] = embedding;
    }

    /// <inheritdoc />
    public void Clear() => _cache.Clear();

    /// <summary>
    /// Computes a SHA256 hash of the text for use as a cache key.
    /// </summary>
    private static string ComputeHash(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
