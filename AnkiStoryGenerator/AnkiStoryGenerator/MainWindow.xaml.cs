using AnkiStoryGenerator.Utilities;
using AnkiStoryGenerator.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using OpenAI;
using OpenAI.Chat;
using PropertyChanged;
using System.ClientModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using ScribanTemplate = Scriban.Template;

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

        await UpdateChatGptPrompt();
    }

    private async void GenerateStory_OnClick(object sender, RoutedEventArgs e)
    {
        var story = await GenerateStoryUsingChatGptApi();

        this._latestStoryHtml = story.OriginalHtml;

        var storyHtmlWithTooltips = AddTooltipsForFlashcardWords(story.OriginalHtml, _viewModel.Flashcards);
        var translationHtmlWithTooltips = AddTooltipsForFlashcardWords(story.TranslatedHtml, _viewModel.Flashcards);

        await SetPreviewWindowHtml(WebViewControlOriginal, storyHtmlWithTooltips);
        await SetPreviewWindowHtml(WebViewControlTranslation, translationHtmlWithTooltips);
    }

    private static string AddTooltipsForFlashcardWords(string openAiResponse, ObservableCollection<FlashcardViewModel> viewModelFlashcards)
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

    private async Task<GeneratedStory> GenerateStoryUsingChatGptApi()
    {
        var appSettings = new Settings();
        var openAiClientOptions = new OpenAIClientOptions() { OrganizationId = appSettings.OpenAiOrganization };

        ChatClient client = new(model: Settings.OpenAiModelId, new ApiKeyCredential(appSettings.OpenAiDeveloperKey), openAiClientOptions);

        // generate story
        var storyCacheFileName = $"{Settings.OpenAiModelId}_{(_viewModel.ChatGptPrompt ?? "").GetHashCode()}.txt";
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
        var translationPrompt = await GetStoryTranslationPrompt(generatedStoryHtmlUnwrapped);

        var translationCacheFileName = $"{Settings.OpenAiModelId}_{(translationPrompt ?? "").GetHashCode()}.txt";
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

    private async Task UpdateChatGptPrompt()
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prompts\\GenerateStoryPrompt.sbn");
        var templateContent = await File.ReadAllTextAsync(templatePath);

        var model = new GenerateStoryParametersModel(_viewModel.LearnedLanguage, _viewModel.NativeLanguage, _viewModel.Genre, _viewModel.PreferredLengthOfAStoryInWords, _viewModel.Flashcards);
        var template = ScribanTemplate.Parse(templateContent, templatePath);

        _viewModel.ChatGptPrompt = await template.RenderAsync(model, x => x.Name);
    }

    private async Task<string> GetStoryTranslationPrompt(string originalStoryHtml)
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prompts\\TranslateStoryPrompt.sbn");
        var templateContent = await File.ReadAllTextAsync(templatePath);

        var model = new TranslateStoryParametersModel(_viewModel.LearnedLanguage, "Polish", originalStoryHtml);
        var template = ScribanTemplate.Parse(templateContent, templatePath);

        var translationPrompt = await template.RenderAsync(model, x => x.Name);
        return translationPrompt;
    }

    private async void PlayStory_OnClick(object sender, RoutedEventArgs e)
    {
        if (this._latestStoryHtml is null)
        {
            MessageBox.Show("Please generate a story first.");
            return;
        }

        var storyInPlainText = HtmlHelpers.ConvertToPlainText(this._latestStoryHtml);

        var cacheFileName = Path.Combine(Settings.AudioFilesCacheDirectory, storyInPlainText.GetHashCode() + ".mp3");
        if (!File.Exists(cacheFileName))
        {
            var ttsAudio = await TextToSpeechHelpers.SynthesizeTextToSpeech(storyInPlainText);
            await File.WriteAllBytesAsync(cacheFileName, ttsAudio);
        }

        var startInfo = new ProcessStartInfo(cacheFileName) { UseShellExecute = true };
        Process.Start(startInfo);
    }

}

internal record GenerateStoryParametersModel(
    string Language,
    string HintLanguage,
    string Genre,
    int PreferredLengthOfAStoryInWords,
    ObservableCollection<FlashcardViewModel> Flashcards);

internal record TranslateStoryParametersModel(string FromLanguage, string ToLanguage, string InputHtml);

internal record GeneratedStory(string OriginalHtml, string TranslatedHtml);
