using System.Threading.Tasks;

namespace Aesir.Client.Services;

public interface IMarkdownService
{
    Task<string> RenderMarkdownAsHtmlAsync(string markdown);
}