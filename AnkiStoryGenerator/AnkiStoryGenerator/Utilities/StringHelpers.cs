using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AnkiStoryGenerator.Utilities;

public static partial class StringHelpers
{
    [GeneratedRegex(@"```html(.*)```\Z", RegexOptions.Singleline)]
    private static partial Regex BackticksRegex();

    /// <summary>
    ///     Removes the triple backticks and the content type from the string (both at the beginning and at the end of a
    ///     string) if it exists.
    /// 
    ///     Input example:
    ///     ```html
    ///     <p>Content</p>
    ///     ```
    /// 
    ///     Output example:
    ///     <p>Content</p>
    /// </summary>
    public static string RemoveBackticksBlockWrapper(string input)
    {
        // Use regular expression to find matches
        var match = BackticksRegex().Match(input);

        // Return the first group of the match if it's successful

        if (match.Success)
        {
            return match.Groups[1].Value.Trim('`', '\n', '\r', '\t', ' ').Trim();
        }

        return input;
    }

    /// <summary>
    /// string.GetHashCode() in modern .NET is not stable, i.e. it gives different results after the application is restarted.
    /// I need a stable hash to help me create a cache key for long content like ChatGPT prompts and use as filename, so this helper method helps achieve that.
    /// </summary>
    public static string GetHashCodeStable(this string input)
    {
        var stableHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var stableHash = BitConverter.ToString(stableHashBytes).Replace("-", string.Empty);
        return stableHash;
    }
}
