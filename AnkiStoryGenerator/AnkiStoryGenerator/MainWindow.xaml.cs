using AnkiStoryGenerator.Services;
using AnkiStoryGenerator.ViewModels;
using Microsoft.Web.WebView2.Core;
using PropertyChanged;
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

        public async Task SetPreviewWindowHtml()
        {
            var htmlContent = "<html><body><h1>Hello, World!</h1></body></html>";

            // Ensure that CoreWebView2 is initialized
            if (WebViewControl.Source is null)
            {

                var options = new CoreWebView2EnvironmentOptions();
                var environment = await CoreWebView2Environment.CreateAsync(null, null, options);
                await WebViewControl.EnsureCoreWebView2Async(environment);
            }

            WebViewControl.NavigateToString(htmlContent);
        }


        private async void LoadFlashcards_OnClick(object sender, RoutedEventArgs e)
        {
            var ankiService = new AnkiService();
            var flashcards = ankiService.GetRecentlyReviewedCardsFromSpecificDeck(AnkiService.AnkiDatabaseFilePath, viewModel.DeckName, viewModel.NumRecentFlashcardsToUse);

            this.viewModel.Flashcards.Clear();
            foreach (var flashcard in flashcards)
            {
                this.viewModel.Flashcards.Add(new FlashcardViewModel(flashcard.Question, flashcard.Answer));
            }

            await SetPreviewWindowHtml();

        }

        private void GenerateStory_OnClick(object sender, RoutedEventArgs e)
        {
            // hardcoded for faster feedback loop
            var templatePath = "D:\\Projekty\\AnkiStoryGenerator\\AnkiStoryGenerator\\AnkiStoryGenerator\\Prompts\\GenerateStoryPrompt.sbn";

            var templateContent = File.ReadAllText(templatePath);

            var model = new GenerateStoryParametersModel("John Doe", new List<string> { "Item1", "Item2", "Item3" });
            var template = ScribanTemplate.Parse(templateContent, templatePath);
            string result = template.Render(model, x => x.Name);

            viewModel.ChatGptPrompt = result;
        }
    }

    internal record GenerateStoryParametersModel(string Name, List<string> Items);
}