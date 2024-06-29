using System.IO;
using System.Net.Http;

namespace AnkiStoryGenerator.Utilities;
internal class TextToSpeechHelpers
{
    internal static async Task<byte[]> SynthesizeTextToSpeech(string text)
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
