using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

[Experimental("SKEXP0001")]
public interface IImageDataLoaderService<TKey, TRecord>
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    Task LoadImageAsync(LoadImageRequest request, CancellationToken cancellationToken);
}


public sealed class LoadImageRequest
{
    public string? ImageLocalPath { get; set; }
    
    public string? ImageFileName { get; set; }
    
    public IDictionary<string, object>? Metadata { get; set; }
}