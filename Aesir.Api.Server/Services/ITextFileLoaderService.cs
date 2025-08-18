using System.Diagnostics.CodeAnalysis;
using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Services;

[Experimental("SKEXP0001")]
public interface ITextFileLoaderService<TKey, TRecord>
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    Task LoadTextFileAsync(LoadTextFileRequest request, CancellationToken cancellationToken);
}

public sealed class LoadTextFileRequest
{
    public string? TextFileLocalPath { get; set; }
    
    public string? TextFileFileName { get; set; }
    
    public IDictionary<string, object>? Metadata { get; set; }
}