using AnkiStoryGenerator.ViewModels;
using Scriban;
using System.IO;

namespace AnkiStoryGenerator;

public static class promp
{
    public static async Task UpdateChatGptPrompt(MainWindowViewModel viewModel)
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prompts\\GenerateStoryPrompt.sbn");
        var templateContent = await File.ReadAllTextAsync(templatePath);

        var model = new GenerateStoryParametersModel(viewModel.LearnedLanguage, viewModel.NativeLanguage, viewModel.Genre, viewModel.PreferredLengthOfAStoryInWords, viewModel.Flashcards);
        var template = Template.Parse(templateContent, templatePath);

        viewModel.ChatGptPrompt = await template.RenderAsync(model, x => x.Name);
    }
}