using AnkiStoryGenerator.ViewModels;
using System.Collections.ObjectModel;
using System.IO;

namespace AnkiStoryGenerator.Utilities;

public static class TooltipsHelper
{
    public static string AddInteractiveTooltipsMarkupToTheStory(string openAiResponse, ObservableCollection<FlashcardViewModel> viewModelFlashcards)
    {
        // OpenAi was asked to respond with a story with basic HTML markup: paragraphs, and requested words highlighted with `<b data-id="{WordNumericId}"></b>`.
        // Now we need to:
        // - include CSS styles
        // - include JavaScript to show tooltips on hover over the highlighted words

        var story = openAiResponse;

        foreach (var flashcard in viewModelFlashcards)
        {
            var tooltipContent = $"{flashcard.WordInNativeLanguage}<hr />{flashcard.WordInLearnedLanguage}";
            story = story.Replace($"data-id=\"{flashcard.Id}\"", $"data-id=\"{flashcard.Id}\" data-tooltip=\"{tooltipContent}\"");
        }

        var htmlDocument = $@"<!DOCTYPE html>
<html>
<head>
  <style type=""text/css"">
{File.ReadAllText(Settings.TooltipStylesPath)}
  </style>
</head>
<body>
{story}
<script>
{File.ReadAllText(Settings.TooltipScriptPath)}
</script>
</body>
</html>
";
        return htmlDocument;
    }
}
