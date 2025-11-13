using System.Diagnostics.CodeAnalysis;
using Aesir.Infrastructure.Models;
using Aesir.Modules.Documents.Models;

namespace Aesir.Modules.Documents.Services.DocumentLoaders;

[Experimental("SKEXP0001")]
public interface ITextFileLoaderService<TKey, TRecord>
    where TKey : notnull
    where TRecord : AesirTextData<TKey>
{
    Task LoadTextFileAsync(LoadTextFileRequest request, CancellationToken cancellationToken);
}