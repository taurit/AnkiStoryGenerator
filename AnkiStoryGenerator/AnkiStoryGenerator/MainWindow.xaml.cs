using AnkiStoryGenerator.ViewModels;
using Microsoft.Web.WebView2.Core;
using PropertyChanged;
using System.Windows;

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
            viewModel.ChatGptPrompt = "yo dawg";
            await SetPreviewWindowHtml();
        }
    }
}