using Microsoft.Extensions.Configuration;

namespace AnkiStoryGenerator;

public class Settings
{
#if !DEBUG
    public const string OpenAiModelId = "gpt-3.5-turbo";
#else
    public const string OpenAiModelId = "gpt-4o";

    // Requires some blind test, but at first glance, stories generated with GPT-4 seem constructed much better (although latency is high and cost much higher). This contradicts marketing claims that GPT-4o is better than GPT-4.
    // public const string OpenAiModelId = "gpt-4"; 
#endif

    // hardcoded for simplicity in the proof-of-concept phase
    public const string TooltipScriptPath = "d:\\Projekty\\AnkiStoryGenerator\\WordExplainerScript\\script.js";

    // hardcoded for simplicity in the proof-of-concept phase
    public const string TooltipStylesPath = "d:\\Projekty\\AnkiStoryGenerator\\WordExplainerScript\\script.css";

    // hardcoded for simplicity in the proof-of-concept phase
    public const string AnkiDatabaseFilePath = "d:\\Projekty\\AnkiStoryGenerator\\LocalDevData\\collection.anki2";

    // hardcoded for simplicity in the proof-of-concept phase
    public const string AudioFilesCacheDirectory = "s:\\Caches\\AnkiStoryGeneratorAudioCache\\";

    // hardcoded for simplicity in the proof-of-concept phase
    public const string GptResponseCacheDirectory = "s:\\Caches\\AnkiStoryGeneratorGptResponseCache\\";

    public readonly string OpenAiDeveloperKey;
    public readonly string OpenAiOrganization;

    public readonly string AzureTtsRegion;
    public readonly string AzureTtsKey;


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
