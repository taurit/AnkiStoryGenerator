using AnkiStoryGenerator.Services;
using AnkiStoryGenerator.ViewModels;
using Microsoft.Web.WebView2.Core;
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

        public async Task SetPreviewWindowHtml(string htmlContentToSet)
        {
            // Ensure that CoreWebView2 is initialized
            if (WebViewControl.Source is null)
            {

                var options = new CoreWebView2EnvironmentOptions();
                var environment = await CoreWebView2Environment.CreateAsync(null, null, options);
                await WebViewControl.EnsureCoreWebView2Async(environment);
            }

            WebViewControl.NavigateToString(htmlContentToSet);
        }


        private void LoadFlashcards_OnClick(object sender, RoutedEventArgs e)
        {
            var ankiService = new AnkiService();
            var flashcards = ankiService.GetRecentlyReviewedCardsFromSpecificDeck(AnkiService.AnkiDatabaseFilePath, viewModel.DeckName, viewModel.NumRecentFlashcardsToUse);

            this.viewModel.Flashcards.Clear();
            int locallyUniqueId = 1;
            foreach (var flashcard in flashcards)
            {
                this.viewModel.Flashcards.Add(new FlashcardViewModel(locallyUniqueId++, flashcard.Question, flashcard.Answer));
            }
        }

        private async void GenerateStory_OnClick(object sender, RoutedEventArgs e)
        {
            // hardcoded for faster feedback loop
            var templatePath = "D:\\Projekty\\AnkiStoryGenerator\\AnkiStoryGenerator\\AnkiStoryGenerator\\Prompts\\GenerateStoryPrompt.sbn";

            var templateContent = await File.ReadAllTextAsync(templatePath);

            var model = new GenerateStoryParametersModel(viewModel.Language, viewModel.Genre, viewModel.PreferredLengthOfAStoryInWords, viewModel.Flashcards);
            var template = ScribanTemplate.Parse(templateContent, templatePath);

            viewModel.ChatGptPrompt = await template.RenderAsync(model, x => x.Name);

            var appSettings = new Settings();
            var openAiClientOptions = new OpenAIClientOptions()
            {
                OrganizationId = appSettings.OpenAiOrganization
            };
            var gpt40ModelId = "gpt-4o";
            var gpt35ModelId = "gpt-3.5-turbo";
            ChatClient client = new(model: gpt35ModelId, new ApiKeyCredential(appSettings.OpenAiDeveloperKey), openAiClientOptions);

            var cheapDevPrompt = "Say 'this is a test.'";
            ChatCompletion completion = await client.CompleteChatAsync(viewModel.ChatGptPrompt);
            var openAiResponse = completion.Content.First().Text;
            const double gpt4OInputTokenPrice = (5.0 / 1_000_000);
            const double gpt4OOutputTokenPrice = (15.0 / 1_000_000);
            var chatGptApiQueryCost = completion.Usage.InputTokens * gpt4OInputTokenPrice + completion.Usage.OutputTokens * gpt4OOutputTokenPrice;

            Debug.WriteLine("Raw HTML response: " + openAiResponse);

            Debug.WriteLine("ChatGPT input tokens used: " + completion.Usage.InputTokens);
            Debug.WriteLine("ChatGPT output tokens used: " + completion.Usage.OutputTokens);
            Debug.WriteLine("ChatGPT API query cost: $" + chatGptApiQueryCost.ToString("F6"));

            await SetPreviewWindowHtml(openAiResponse);
        }
    }

    internal record GenerateStoryParametersModel(string Language, string Genre, int PreferredLengthOfAStoryInWords, ObservableCollection<FlashcardViewModel> Flashcards);
}