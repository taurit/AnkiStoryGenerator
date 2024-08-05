using System.IO;
using System.ServiceModel.Syndication;
using System.Text.Json;
using System.Xml;

namespace AnkiStoryGenerator.Utilities;

internal record PodcastEpisode(string StoryTitle, string StoryHtmlSpanish, string StoryHtmlPolish, string AudioSpanishFilePath, DateTimeOffset Created);

internal class RssHelper
{
    // Internal format to keep the state of all published episodes
    private static readonly string EpisodesLocalFilePath = Path.Combine(Settings.RssFeedFolder, "episodes.json");

    // A public feed of episodes, re-generated from scratch every time a new episode is added (rather than updated)
    private static readonly string RssFeedLocalFilePath = Path.Combine(Settings.RssFeedFolder, "stories.xml");

    internal void AddEpisodeToTheFeedInputData(PodcastEpisode podcastEpisode)
    {
        // load and deserialize the episodes from `EpisodesLocalFilePath`
        var episodes = LoadPodcastEpisodesFromInternalDatabase();

        // add the new episode to the list
        episodes.Add(podcastEpisode);

        // serialize and save the episodes back to `EpisodesLocalFilePath`
        var serializedEpisodes = JsonSerializer.Serialize(episodes);
        File.WriteAllText(EpisodesLocalFilePath, serializedEpisodes);
    }


    internal void GenerateRssFeed()
    {
        // Load episodes from internal database
        var episodes = LoadPodcastEpisodesFromInternalDatabase();

        // Create feed items (episodes)
        var rssItems = new List<SyndicationItem>();

        foreach (var episode in episodes)
        {
            var newRssItem = CreatePodcastItem(
                title: $"{episode.StoryTitle}",
                description: episode.StoryHtmlSpanish,
                url: episode.AudioSpanishFilePath,
                length: new FileInfo(episode.AudioSpanishFilePath).Length,
                pubDate: episode.Created.UtcDateTime
            );
            rssItems.Add(newRssItem);
        }

        // Create the podcast feed
        var feed = new SyndicationFeed(
            "Anki Stories",
            "A proof-of-concept feed for Anki Story Generator.",
            new Uri("https://example.com/podcast"),
            rssItems
        )
        {
            Copyright = new TextSyndicationContent("© 2024 Taurit"),
            Language = "es-ES",
            LastUpdatedTime = DateTimeOffset.Now
        };

        // Generate the RSS XML
        using (var writer = XmlWriter.Create(RssFeedLocalFilePath))
        {
            var rssFormatter = new Rss20FeedFormatter(feed);
            rssFormatter.WriteTo(writer);
        }

        Console.WriteLine("Podcast feed generated successfully.");
    }

    static SyndicationItem CreatePodcastItem(string title, string description, string url, long length, DateTime pubDate)
    {
        var item = new SyndicationItem(
            title,
            description,
            new Uri(url),
            Guid.NewGuid().ToString(),
            new DateTimeOffset(pubDate)
        );

        var enclosure = new SyndicationLink(new Uri(url), "enclosure", title, "audio/mpeg", length);
        item.Links.Add(enclosure);

        return item;
    }


    private static List<PodcastEpisode> LoadPodcastEpisodesFromInternalDatabase()
    {
        var episodes = new List<PodcastEpisode>();
        if (File.Exists(EpisodesLocalFilePath))
        {
            var json = File.ReadAllText(EpisodesLocalFilePath);
            var episodesFromFile = JsonSerializer.Deserialize<List<PodcastEpisode>>(json);
            if (episodesFromFile != null)
            {
                episodes.AddRange(episodesFromFile);
            }
        }

        return episodes;
    }
}
