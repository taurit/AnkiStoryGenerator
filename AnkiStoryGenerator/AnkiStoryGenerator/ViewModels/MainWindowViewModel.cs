﻿using PropertyChanged;
using System.Collections.ObjectModel;

namespace AnkiStoryGenerator.ViewModels;

[AddINotifyPropertyChangedInterface]
public class MainWindowViewModel
{
    public string Language { get; } = "Spanish"; // hardcoded, but in the future, this could be a dropdown
    public string DeckName { get; } = "1. Spanish"; // hardcoded, but in the future, this could be a dropdown
    public string Genre { get; } = "Crime"; // hardcoded, but in the future, this could be a dropdown

    public int NumRecentFlashcardsToUse { get; } = 10;
    public int PreferredLengthOfAStoryInWords { get; } = 400;

    public ObservableCollection<FlashcardViewModel> Flashcards { get; set; } = new ObservableCollection<FlashcardViewModel>();
    public string? ChatGptPrompt { get; set; }
}

[AddINotifyPropertyChangedInterface]
public sealed class FlashcardViewModel(string question, string answer)
{
    public string Question { get; } = question;
    public string Answer { get; } = answer;

}