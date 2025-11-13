namespace Aesir.Common;

public static class StringExtensions
{
    /// <summary>
    /// Normalizes line endings in the input string to Unix-style (\n).
    /// Replaces \r\n (Windows) and \r (classic Mac) with \n.
    /// </summary>
    /// <param name="input">The input string to normalize.</param>
    /// <returns>The normalized string.</returns>
    public static string NormalizeLineEndings(this string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Replace Windows line endings first, then any remaining standalone \r
        return input.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}