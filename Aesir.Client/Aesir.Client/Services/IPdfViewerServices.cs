using System.Threading.Tasks;

namespace Aesir.Client.Services;

public interface IPdfViewerService
{
    Task ShowPdfAsync(string fileUri);
}