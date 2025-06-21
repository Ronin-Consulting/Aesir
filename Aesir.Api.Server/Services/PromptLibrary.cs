using Aesir.Common.Prompts;

namespace Aesir.Api.Server.Services;

public class PromptLibrary
{
    private static readonly IPromptProvider _promptProvider = new DefaultPromptProvider();

    public static string DefaultSystemPromptTemplate => 
        _promptProvider.GetSystemPrompt(PromptContext.Business).Content;

    public static string DefaultCondensePromptTemplate => 
        _promptProvider.GetCondensePrompt().Content;
}