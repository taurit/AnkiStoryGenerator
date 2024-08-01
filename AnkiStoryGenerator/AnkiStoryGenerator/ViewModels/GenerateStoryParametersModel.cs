using System.Collections.ObjectModel;

namespace AnkiStoryGenerator.ViewModels;

internal record GenerateStoryParametersModel(
    string Language,
    string HintLanguage,
    string Genre,
    int PreferredLengthOfAStoryInWords,
    ObservableCollection<FlashcardViewModel> Flashcards);
