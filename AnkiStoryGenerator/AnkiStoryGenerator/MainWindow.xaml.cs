using AnkiStoryGenerator.Services;
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

namespace AnkiStoryGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class MainWindow : Window
    {
        MainWindowViewModel viewModel = new MainWindowViewModel();

        public MainWindow()
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        public async Task SetPreviewWindowHtml(WebView2 webViewControl, string htmlContentToSet)
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
            var ankiService = new AnkiService();
            var flashcards = ankiService.GetRecentlyReviewedCardsFromSpecificDeck(AnkiService.AnkiDatabaseFilePath, viewModel.DeckName, viewModel.NumRecentFlashcardsToUse);

            this.viewModel.Flashcards.Clear();
            int locallyUniqueId = 1;
            foreach (var flashcard in flashcards)
            {
                this.viewModel.Flashcards.Add(new FlashcardViewModel(locallyUniqueId++, flashcard.Question, flashcard.Answer));
            }
            await UpdateChatGptPrompt();
        }

        private async void GenerateStory_OnClick(object sender, RoutedEventArgs e)
        {
            var story = await GenerateStoryUsingChatGptApi();
            var storyHtmlWithTooltips = AddTooltipsForFlashcardWords(story.OriginalHtml, viewModel.Flashcards);
            var translationHtmlWithTooltips = AddTooltipsForFlashcardWords(story.TranslatedHtml, viewModel.Flashcards);

            await SetPreviewWindowHtml(WebViewControlOriginal, storyHtmlWithTooltips);
            await SetPreviewWindowHtml(WebViewControlTranslation, translationHtmlWithTooltips);
        }

        private string AddTooltipsForFlashcardWords(string openAiResponse, ObservableCollection<FlashcardViewModel> viewModelFlashcards)
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
            ChatCompletion completion = await client.CompleteChatAsync(viewModel.ChatGptPrompt);
            var generatedStoryHtml = completion.Content.First().Text;

            // translate story
            var translationPrompt = await GetStoryTranslationPrompt(generatedStoryHtml);
            ChatCompletion translationCompletion = await client.CompleteChatAsync(translationPrompt);
            var translatedStoryHtml = translationCompletion.Content.First().Text;

            var chatGptApiQueryCost = (completion.Usage.InputTokens + translationCompletion.Usage.InputTokens) * Settings.InputTokenPrice +
                                      (completion.Usage.OutputTokens + translationCompletion.Usage.OutputTokens) * Settings.OutputTokenPrice;
            Debug.WriteLine("ChatGPT API query cost: $" + chatGptApiQueryCost.ToString("F6"));

            return new GeneratedStory(generatedStoryHtml, translatedStoryHtml);
        }

        private async Task UpdateChatGptPrompt()
        {
            // hardcoded for faster feedback loop
            var templatePath = "D:\\Projekty\\AnkiStoryGenerator\\AnkiStoryGenerator\\AnkiStoryGenerator\\Prompts\\GenerateStoryPrompt.sbn";
            var templateContent = await File.ReadAllTextAsync(templatePath);

            var model = new GenerateStoryParametersModel(viewModel.Language, viewModel.Genre, viewModel.PreferredLengthOfAStoryInWords, viewModel.Flashcards);
            var template = ScribanTemplate.Parse(templateContent, templatePath);

            viewModel.ChatGptPrompt = await template.RenderAsync(model, x => x.Name);
        }

        private async Task<string> GetStoryTranslationPrompt(string originalStoryHtml)
        {
            var templatePath = "D:\\Projekty\\AnkiStoryGenerator\\AnkiStoryGenerator\\AnkiStoryGenerator\\Prompts\\TranslateStoryPrompt.sbn";
            var templateContent = await File.ReadAllTextAsync(templatePath);

            var model = new TranslateStoryParametersModel(viewModel.Language, "Polish", originalStoryHtml);
            var template = ScribanTemplate.Parse(templateContent, templatePath);

            var translationPrompt = await template.RenderAsync(model, x => x.Name);
            return translationPrompt;
        }
    }

    internal record GenerateStoryParametersModel(string Language, string Genre, int PreferredLengthOfAStoryInWords, ObservableCollection<FlashcardViewModel> Flashcards);
    internal record TranslateStoryParametersModel(string FromLanguage, string ToLanguage, string InputHtml);
    internal record GeneratedStory(string OriginalHtml, string TranslatedHtml);
}