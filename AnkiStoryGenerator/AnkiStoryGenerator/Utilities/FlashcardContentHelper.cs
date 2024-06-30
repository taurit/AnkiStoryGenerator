using System.Text.RegularExpressions;

namespace AnkiStoryGenerator.Utilities;

/// <summary>
/// Helps transform the content of flashcard questions and answers (often unstructured and containing HTML tags and comments in mixed language) to a plain text, more suitable as input for GPT-4 Story Generator.
/// </summary>
internal static class FlashcardContentHelper
{
    public static List<Flashcard> HeuristicallySanitizeFlashcards(List<Flashcard> flashcards)
    {
        var updatedFlashcards = new List<Flashcard>();
        foreach (var flashcard in flashcards)
        {
            var sanitizedQuestion = FlashcardContentHelper.RemoveHtmlTags(flashcard.WordInLearnedLanguage);
            var sanitizedAnswer = FlashcardContentHelper.RemoveHtmlTags(flashcard.WordInUserNativeLanguage);
            var sanitizedFlashcard = new Flashcard(sanitizedQuestion, sanitizedAnswer);
            updatedFlashcards.Add(sanitizedFlashcard);
        }

        return updatedFlashcards;
    }

    /// <summary>
    /// Converts a string with HTML tags (images, font styles) to a plain text.
    /// </summary>
    private static string RemoveHtmlTags(string input)
    {
        // replace <br />, <br/>, <br> (case-insensitive) with new line
        input = Regex.Replace(input, @"<br\s*/?>", Environment.NewLine, RegexOptions.IgnoreCase);

        // strip all other html tags
        var plainText = HtmlHelpers.ConvertToPlainText(input).Trim();

        // keep only the first line - heuristics, assuming the rest is usually some comments and detailed explanation
        var plainTextLines = plainText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        var firstLine = plainTextLines.First();



        return firstLine;
    }

}
