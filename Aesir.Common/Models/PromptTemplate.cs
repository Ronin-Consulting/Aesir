namespace Aesir.Common.Models;

public class PromptTemplate
{
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, string> Variables { get; set; } = new();

    public PromptTemplate(string content)
    {
        Content = content;
    }

    public string Render(Dictionary<string, string>? variables = null)
    {
        var result = Content;
        var allVariables = Variables.Concat(variables ?? new Dictionary<string, string>())
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        foreach (var variable in allVariables)
        {
            result = result.Replace($"{{{variable.Key}}}", variable.Value);
        }

        return result;
    }

    public static implicit operator PromptTemplate(string content) => new(content);
}