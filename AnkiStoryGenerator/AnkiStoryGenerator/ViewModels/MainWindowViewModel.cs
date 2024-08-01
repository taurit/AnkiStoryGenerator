using AnkiStoryGenerator.Utilities;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.IO;

namespace AnkiStoryGenerator.ViewModels;

[AddINotifyPropertyChangedInterface]
public class MainWindowViewModel
{
    public string LearnedLanguage { get; } = "Spanish (Castillan)"; // "Spanish (Castillan)"; // hardcoded, but in the future, this could be a dropdown
    public string NativeLanguage { get; } = "Polish"; // hardcoded, but in the future, this could be a dropdown
    public string DeckName { get; } = "1. Spanish"; // "3. English"; // hardcoded, but in the future, this could be a dropdown
    public string Genre { get; set; }

    public int NumRecentFlashcardsToUse { get; } = 20;
    public int PreferredLengthOfAStoryInWords { get; } = 200;

    public ObservableCollection<FlashcardViewModel> Flashcards { get; set; } = [];
    public string? ChatGptPrompt { get; set; }

    public string? LatestStoryHtml { get; set; }

    [DependsOn(nameof(LatestStoryHtml))]
    public string LatestStoryPlainText => HtmlHelpers.ConvertToPlainText(LatestStoryHtml);

    [DependsOn(nameof(LatestStoryPlainText))]
    public string LatestStoryAudioFileName => Path.Combine(Settings.AudioFilesCacheDirectory, LatestStoryPlainText.GetHashCodeStable() + ".mp3");
}

[AddINotifyPropertyChangedInterface]
public sealed class FlashcardViewModel(int id, string wordInLearnedLanguage, string wordInNativeLanguage)
{
    // Be careful renaming this property, as it is used in the Scriban template!
    public int Id { get; } = id;

    // Be careful renaming this property, as it is used in the Scriban template!
    public string WordInLearnedLanguage { get; } = wordInLearnedLanguage;

    // Be careful renaming this property, as it is used in the Scriban template!
    public string WordInNativeLanguage { get; } = wordInNativeLanguage;
}
