using System.Collections.Generic;

namespace Aesir.Client.Services;

public interface IContentProcessingService
{
    string ProcessThinkingModelContent(string input);
    void HandleLinkClick(string link, Dictionary<string, string> attributes);
}