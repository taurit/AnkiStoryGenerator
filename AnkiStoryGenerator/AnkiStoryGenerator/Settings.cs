using Microsoft.Extensions.Configuration;
using System.IO;

namespace AnkiStoryGenerator;

public class Settings
{
#if !DEBUG
    public const string OpenAiModelId = "gpt-4o-mini";
#else
    public const string OpenAiModelId = "gpt-4o";

    // Requires some blind test, but at first glance, stories generated with GPT-4 seem constructed much better (although latency is high and cost much higher). This contradicts marketing claims that GPT-4o is better than GPT-4.
    // public const string OpenAiModelId = "gpt-4"; 
#endif

    // hardcoded for simplicity in the proof-of-concept phase
    public static string TooltipScriptPath = ReturnFirstFileThatExists([
        "d:\\Projekty\\AnkiStoryGenerator\\WordExplainerScript\\script.js",
        "d:\\Projects\\AnkiStoryGenerator\\WordExplainerScript\\script.js"
    ]);

    // hardcoded for simplicity in the proof-of-concept phase
    public static string TooltipStylesPath = ReturnFirstFileThatExists([
        "d:\\Projekty\\AnkiStoryGenerator\\WordExplainerScript\\script.css",
        "d:\\Projects\\AnkiStoryGenerator\\WordExplainerScript\\script.css"
    ]);

    // hardcoded for simplicity in the proof-of-concept phase
    public static string AnkiDatabaseFilePath = ReturnFirstFileThatExists([
        "c:\\Users\\windo\\AppData\\Roaming\\Anki2\\Usuario 1\\collection.anki2", // stationary pc
        "c:\\Users\\windo\\AppData\\Roaming\\Anki2\\User 1\\collection.anki2"     // dell laptop
    ]);

    // rss feed file path
    public static string RssFeedFolder = ReturnFirstDirectoryThatExists([
        "s:\\Caches\\AnkiStoryGeneratorRssCache\\", // stationary pc
    ]);

    // hardcoded for simplicity in the proof-of-concept phase
    public static string AudioFilesCacheDirectory = ReturnFirstDirectoryThatExists([
        "s:\\Caches\\AnkiStoryGeneratorAudioCache\\",
        "d:\\Projects\\Caches\\AnkiStoryGeneratorAudioCache\\"
    ]);

    // hardcoded for simplicity in the proof-of-concept phase
    public static string GptResponseCacheDirectory = ReturnFirstDirectoryThatExists([
        "s:\\Caches\\AnkiStoryGeneratorGptResponseCache\\",
        "d:\\Projects\\Caches\\AnkiStoryGeneratorGptResponseCache\\"
    ]);

    public readonly string OpenAiDeveloperKey;
    public readonly string OpenAiOrganization;

    public readonly string AzureTtsRegion;
    public readonly string AzureTtsKey;

    private static string ReturnFirstFileThatExists(string[] paths)
    {
        foreach (var path in paths)
        {
            if (File.Exists(path)) return path;
        }

        throw new Exception("None of the files exists:\n" + String.Join("\n", paths));
    }

    private static string ReturnFirstDirectoryThatExists(string[] paths)
    {
        foreach (var path in paths)
        {
            if (Directory.Exists(path)) return path;
        }

        throw new Exception("None of the directories exists:\n" + String.Join("\n", paths));
    }


    public Settings()
    {
        var builder = new ConfigurationBuilder().AddUserSecrets<Settings>();
        var configuration = builder.Build();
        OpenAiDeveloperKey = configuration["OpenAiDeveloperKey"] ??
                             throw new InvalidOperationException("OpenAiDeveloperKey is missing in User Secrets configuration");
        OpenAiOrganization = configuration["OpenAiOrganization"] ??
                             throw new InvalidOperationException("OpenAiOrganization is missing in User Secrets configuration");

        AzureTtsRegion = configuration["AzureTtsRegion"] ?? throw new InvalidOperationException("AzureTtsRegion is missing in User Secrets configuration");
        AzureTtsKey = configuration["AzureTtsKey"] ?? throw new InvalidOperationException("AzureTtsKey is missing in User Secrets configuration");
    }
}
