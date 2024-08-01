using AnkiStoryGenerator.Utilities;
using AnkiStoryGenerator.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using OpenAI;
using OpenAI.Chat;
using PropertyChanged;
using System.ClientModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace AnkiStoryGenerator;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[AddINotifyPropertyChangedInterface]
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel = new();
    private string? _latestStoryHtml;

    public MainWindow()
    {
        DataContext = _viewModel;
        InitializeComponent();
    }

    private static async Task SetPreviewWindowHtml(WebView2 webViewControl, string htmlContentToSet)
    {
        // Ensure that CoreWebView2 is initialized
        if (webViewControl.Source is null)
        {
            var options = new CoreWebView2EnvironmentOptions();
            var environment = await CoreWebView2Environment.CreateAsync(null, null, options);
            await webViewControl.EnsureCoreWebView2Async(environment);
        }

        webViewControl.NavigateToString(htmlContentToSet);
    }

    private async void LoadFlashcards_OnClick(object sender, RoutedEventArgs e)
    {
        var flashcards =
            AnkiHelpers.GetRecentlyReviewedCardsFromSpecificDeck(Settings.AnkiDatabaseFilePath, _viewModel.DeckName, _viewModel.NumRecentFlashcardsToUse);
        var sanitizedFlashcards = FlashcardContentHelper.HeuristicallySanitizeFlashcards(flashcards);

        this._viewModel.Flashcards.Clear();
        var locallyUniqueId = 1;
        foreach (var flashcard in sanitizedFlashcards)
        {
            var flashcardViewModel = new FlashcardViewModel(locallyUniqueId++, flashcard.WordInLearnedLanguage, flashcard.WordInUserNativeLanguage);
            this._viewModel.Flashcards.Add(flashcardViewModel);
        }

        await promp.UpdateChatGptPrompt(_viewModel);
    }

    private async void GenerateStory_OnClick(object sender, RoutedEventArgs e)
    {
        var story = await GenerateStoryUsingChatGptApi();

        this._latestStoryHtml = story.OriginalHtml;

        var storyHtmlWithTooltips = TooltipsHelper.AddInteractiveTooltipsMarkupToTheStory(story.OriginalHtml, _viewModel.Flashcards);
        var translationHtmlWithTooltips = TooltipsHelper.AddInteractiveTooltipsMarkupToTheStory(story.TranslatedHtml, _viewModel.Flashcards);

        await SetPreviewWindowHtml(WebViewControlOriginal, storyHtmlWithTooltips);
        await SetPreviewWindowHtml(WebViewControlTranslation, translationHtmlWithTooltips);
    }

    private async Task<GeneratedStory> GenerateStoryUsingChatGptApi()
    {
        var appSettings = new Settings();
        var openAiClientOptions = new OpenAIClientOptions() { OrganizationId = appSettings.OpenAiOrganization };

        ChatClient client = new(model: Settings.OpenAiModelId, new ApiKeyCredential(appSettings.OpenAiDeveloperKey), openAiClientOptions);

        // generate story
        var storyCacheFileName = $"{Settings.OpenAiModelId}_{(_viewModel.ChatGptPrompt ?? "").GetHashCodeStable()}.txt";
        var storyCacheFilePath = Path.Combine(Settings.GptResponseCacheDirectory, storyCacheFileName);

        if (!File.Exists(storyCacheFilePath))
        {
            ChatCompletion completion = await client.CompleteChatAsync(_viewModel.ChatGptPrompt);
            var generatedStoryHtmlToSave = completion.Content[0].Text;
            await File.WriteAllTextAsync(storyCacheFilePath, generatedStoryHtmlToSave);
        }

        var generatedStoryHtml = await File.ReadAllTextAsync(storyCacheFilePath);
        var generatedStoryHtmlUnwrapped = StringHelpers.RemoveBackticksBlockWrapper(generatedStoryHtml);

        // translate story
        var translationPrompt = await PromptsHelper.GetStoryTranslationPrompt(_viewModel, generatedStoryHtmlUnwrapped);

        var translationCacheFileName = $"{Settings.OpenAiModelId}_{(translationPrompt ?? "").GetHashCodeStable()}.txt";
        var translationCacheFilePath = Path.Combine(Settings.GptResponseCacheDirectory, translationCacheFileName);

        if (!File.Exists(translationCacheFilePath))
        {
            ChatCompletion translationCompletion = await client.CompleteChatAsync(translationPrompt);
            var translatedStoryHtmlToSave = translationCompletion.Content[0].Text;
            await File.WriteAllTextAsync(translationCacheFilePath, translatedStoryHtmlToSave);
        }
        var translatedStoryHtml = await File.ReadAllTextAsync(translationCacheFilePath);
        var translatedStoryHtmlUnwrapped = StringHelpers.RemoveBackticksBlockWrapper(translatedStoryHtml);

        return new GeneratedStory(generatedStoryHtmlUnwrapped, translatedStoryHtmlUnwrapped);
    }

    private async void PlayStory_OnClick(object sender, RoutedEventArgs e)
    {
        if (this._latestStoryHtml is null)
        {
            MessageBox.Show("Please generate a story first.");
            return;
        }

        var storyInPlainText = HtmlHelpers.ConvertToPlainText(this._latestStoryHtml);

        var cacheFileName = Path.Combine(Settings.AudioFilesCacheDirectory, storyInPlainText.GetHashCodeStable() + ".mp3");
        if (!File.Exists(cacheFileName))
        {
            var ttsAudio = await TextToSpeechHelpers.SynthesizeTextToSpeech(storyInPlainText);
            await File.WriteAllBytesAsync(cacheFileName, ttsAudio);
        }

        var startInfo = new ProcessStartInfo(cacheFileName) { UseShellExecute = true };
        Process.Start(startInfo);
    }

    private void PublishToRssFeed_OnClick(object sender, RoutedEventArgs e)
    {
        new RssHelper().CreateFeed();
    }
}
