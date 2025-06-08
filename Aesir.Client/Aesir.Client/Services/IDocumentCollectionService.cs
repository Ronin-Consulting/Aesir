using System.IO;
using System.Threading.Tasks;

namespace Aesir.Client.Services;

public interface IDocumentCollectionService
{
    Task<Stream> GetStreamAsync(string filename);
}