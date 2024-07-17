using System.ServiceModel.Syndication;
using System.Xml;

namespace AnkiStoryGenerator.Utilities;
internal class RssHelper
{
    internal void CreateFeed()
    {
        // Create feed items (episodes)
        var items = new List<SyndicationItem>
        {
            CreatePodcastItem(
                title: "Episode 1",
                description: "This is the first episode.",
                url: "https://example.com/podcasts/episode1.mp3",
                length: 12345678,
                pubDate: DateTime.Now.AddDays(-10)
            ),
            CreatePodcastItem(
                title: "Episode 2",
                description: "This is the second episode.",
                url: "https://example.com/podcasts/episode2.mp3",
                length: 23456789,
                pubDate: DateTime.Now.AddDays(-5)
            )
        };

        // Create the podcast feed
        var feed = new SyndicationFeed(
            "My Podcast",
            "A description of my podcast.",
            new Uri("https://example.com/podcast"),
            items
        )
        {
            Copyright = new TextSyndicationContent("© 2024 My Podcast"),
            Language = "en-US",
            LastUpdatedTime = DateTimeOffset.Now
        };

        // Generate the RSS XML
        using (var writer = XmlWriter.Create("d:/podcast.xml"))
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
}
