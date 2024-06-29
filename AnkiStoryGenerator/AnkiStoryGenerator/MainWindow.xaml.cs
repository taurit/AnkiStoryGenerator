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
using System.Net.Http;
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
            AnkiHelpers.GetRecentlyReviewedCardsFromSpecificDeck(AnkiHelpers.AnkiDatabaseFilePath, _viewModel.DeckName, _viewModel.NumRecentFlashcardsToUse);

        this._viewModel.Flashcards.Clear();
        var locallyUniqueId = 1;
        foreach (var flashcard in flashcards)
        {
            this._viewModel.Flashcards.Add(new FlashcardViewModel(locallyUniqueId++, flashcard.Question, flashcard.Answer));
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
            var tooltipContent = $"{flashcard.Answer}<hr />{flashcard.Question}";
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
        ChatCompletion completion = await client.CompleteChatAsync(_viewModel.ChatGptPrompt);
        var generatedStoryHtml = completion.Content[0].Text;
        var generatedStoryHtmlUnwrapped = StringHelpers.RemoveBackticksBlockWrapper(generatedStoryHtml);

        // translate story
        var translationPrompt = await GetStoryTranslationPrompt(generatedStoryHtmlUnwrapped);
        ChatCompletion translationCompletion = await client.CompleteChatAsync(translationPrompt);
        var translatedStoryHtml = translationCompletion.Content[0].Text;
        var translatedStoryHtmlUnwrapped = StringHelpers.RemoveBackticksBlockWrapper(translatedStoryHtml);

        var chatGptApiQueryCost = (completion.Usage.InputTokens + translationCompletion.Usage.InputTokens) * Settings.InputTokenPrice +
                                  (completion.Usage.OutputTokens + translationCompletion.Usage.OutputTokens) * Settings.OutputTokenPrice;
        Debug.WriteLine("ChatGPT API query cost: $" + chatGptApiQueryCost.ToString("F6"));

        return new GeneratedStory(generatedStoryHtmlUnwrapped, translatedStoryHtmlUnwrapped);
    }

    private async Task UpdateChatGptPrompt()
    {
        // hardcoded for faster feedback loop
        var templatePath = "D:\\Projekty\\AnkiStoryGenerator\\AnkiStoryGenerator\\AnkiStoryGenerator\\Prompts\\GenerateStoryPrompt.sbn";
        var templateContent = await File.ReadAllTextAsync(templatePath);

        var random = new Random();
        var randomGenre = new[] { "fantasy", "sci-fi", "mystery", "horror", "romance", "comedy", "crime" }[random.Next(0, 7)];

        var model = new GenerateStoryParametersModel(_viewModel.Language, randomGenre, _viewModel.PreferredLengthOfAStoryInWords, _viewModel.Flashcards);
        var template = ScribanTemplate.Parse(templateContent, templatePath);

        _viewModel.ChatGptPrompt = await template.RenderAsync(model, x => x.Name);
    }

    private async Task<string> GetStoryTranslationPrompt(string originalStoryHtml)
    {
        var templatePath = "D:\\Projekty\\AnkiStoryGenerator\\AnkiStoryGenerator\\AnkiStoryGenerator\\Prompts\\TranslateStoryPrompt.sbn";
        var templateContent = await File.ReadAllTextAsync(templatePath);

        var model = new TranslateStoryParametersModel(_viewModel.Language, "Polish", originalStoryHtml);
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
        // todo add some caching
        var ttsAudio = await SynthesizeTextToSpeech(storyInPlainText);
        await File.WriteAllBytesAsync("d:/testAAA.mp3", ttsAudio);
        var startInfo = new ProcessStartInfo("d:/testAAA.mp3") { UseShellExecute = true };
        Process.Start(startInfo);
    }

    private static async Task<byte[]> SynthesizeTextToSpeech(string text)
    {
        var settings = new Settings();

        var endpoint = $"https://{settings.AzureTtsRegion}.tts.speech.microsoft.com/cognitiveservices/v1";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", settings.AzureTtsKey);
        client.DefaultRequestHeaders.Add("User-Agent", "AnkiStoryGenerator");
        client.DefaultRequestHeaders.Add("X-Microsoft-OutputFormat", "audio-16khz-128kbitrate-mono-mp3");

        var requestBody = $@"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='es-ES'>
                                            <voice xml:lang='es-ES' xml:gender='Female' name='es-ES-TrianaNeural'>
                                                <prosody rate='0.8'>
                                                   {text}
                                                </prosody>
                                            </voice>
                                        </speak>";

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/ssml+xml");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var audioStream = await response.Content.ReadAsStreamAsync();
        using var memoryStream = new MemoryStream();
        await audioStream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }
}

internal record GenerateStoryParametersModel(
    string Language,
    string Genre,
    int PreferredLengthOfAStoryInWords,
    ObservableCollection<FlashcardViewModel> Flashcards);

internal record TranslateStoryParametersModel(string FromLanguage, string ToLanguage, string InputHtml);

internal record GeneratedStory(string OriginalHtml, string TranslatedHtml);
