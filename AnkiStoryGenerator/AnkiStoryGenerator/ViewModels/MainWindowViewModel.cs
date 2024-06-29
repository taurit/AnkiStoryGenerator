using PropertyChanged;
using System.Collections.ObjectModel;

namespace AnkiStoryGenerator.ViewModels;

[AddINotifyPropertyChangedInterface]
public class MainWindowViewModel
{
    public string Language { get; } = "Spanish (Castillan)"; // hardcoded, but in the future, this could be a dropdown
    public string DeckName { get; } = "1. Spanish"; // hardcoded, but in the future, this could be a dropdown
    public string Genre { get; } = "crime"; // hardcoded, but in the future, this could be a dropdown

    public int NumRecentFlashcardsToUse { get; } = 20;
    public int PreferredLengthOfAStoryInWords { get; } = 200;

    public ObservableCollection<FlashcardViewModel> Flashcards { get; set; } = new ObservableCollection<FlashcardViewModel>();
    public string? ChatGptPrompt { get; set; }
}

[AddINotifyPropertyChangedInterface]
public sealed class FlashcardViewModel(int id, string question, string answer)
{
    public int Id { get; } = id;
    public string Question { get; } = question;
    public string Answer { get; } = answer;

}