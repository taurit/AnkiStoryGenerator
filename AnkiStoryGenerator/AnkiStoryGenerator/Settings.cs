using Microsoft.Extensions.Configuration;

namespace AnkiStoryGenerator;

public class Settings
{
    public readonly string OpenAiDeveloperKey;
    public readonly string OpenAiOrganization;

    public Settings()
    {
        var builder = new ConfigurationBuilder().AddUserSecrets<Settings>();
        var configuration = builder.Build();
        OpenAiDeveloperKey = configuration["OpenAiDeveloperKey"] ??
                             throw new InvalidOperationException(
                                 "OpenAiDeveloperKey is missing in User Secrets configuration");
        OpenAiOrganization = configuration["OpenAiOrganization"] ??
                             throw new InvalidOperationException(
                                 "OpenAiOrganization is missing in User Secrets configuration");
    }
}
