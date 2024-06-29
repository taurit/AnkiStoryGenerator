using System.Text.RegularExpressions;

namespace AnkiStoryGenerator.Services;

public static class StringHelpers
{
    /// <summary>
    ///     Removes the triple backticks and the content type from the string (both at the beginning and at the end of a
    ///     string) if it exists.
    /// </summary>
    public static string TrimBackticksWrapperFromString(string input)
    {
        // Use regular expression to find matches
        var match = Regex.Match(input, @"```html(.*)```\Z", RegexOptions.Singleline);

        // Return the first group of the match if it's successful

        if (match.Success)
            return match.Groups[1].Value.Trim('`', '\n', '\r', '\t', ' ').Trim();
        return input;
    }
}