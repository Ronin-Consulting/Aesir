namespace Aesir.Api.Server.Extensions;

public static class StringExtensions
{
    public static string RemovePartOfCompletionMessage(this string input, string startAt, string endAt)
    {
        // Extract anything between <think> tags (including the tags themselves)
        var startIndex = input.IndexOf(startAt, StringComparison.Ordinal);

        if (startIndex < 0) return input;
        
        var endIndex = input.IndexOf(endAt, startIndex, StringComparison.Ordinal);
        
        if (endIndex >= 0)
        {
            // Remove everything from start of <think> to end of </think> (including tags)
            input = input.Remove(startIndex, (endIndex + endAt.Length) - startIndex);
        }
        else
        {
            // If closing tag is missing, just remove the opening tag as before
            input = input.Replace(startAt, "");
        }

        return input;
    }
}