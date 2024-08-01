using AnkiStoryGenerator.ViewModels;
using Scriban;
using System.IO;

namespace AnkiStoryGenerator.Utilities;

internal static class PromptsHelper
{
    public static async Task<string> GetStoryTranslationPrompt(MainWindowViewModel viewModel, string originalStoryHtml)
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prompts\\TranslateStoryPrompt.sbn");
        var templateContent = await File.ReadAllTextAsync(templatePath);

        var model = new TranslateStoryParametersModel(viewModel.LearnedLanguage, "Polish", originalStoryHtml);
        var template = Template.Parse(templateContent, templatePath);

        var translationPrompt = await template.RenderAsync(model, x => x.Name);
        return translationPrompt;
    }
}
