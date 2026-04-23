using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using NewsApp.Models;

namespace NewsApp.Services
{
    public class RssService : INewsService
    {
        private readonly Dictionary<string, string> _rssFeeds = new()
        {
            { "World", "https://rss.nytimes.com/services/xml/rss/nyt/World.xml" },
            { "Technology", "https://rss.nytimes.com/services/xml/rss/nyt/Technology.xml" },
            { "Business", "https://rss.nytimes.com/services/xml/rss/nyt/Business.xml" },
            { "Sports", "https://rss.nytimes.com/services/xml/rss/nyt/Sports.xml" },
            { "Science", "https://rss.nytimes.com/services/xml/rss/nyt/Science.xml" }
        };

        public async Task<List<Article>> GetHeadlinesAsync(List<string> categories)
        {
            var articles = new List<Article>();

            foreach (var category in categories)
            {
                if (!_rssFeeds.ContainsKey(category)) continue;
                var feedUrl = _rssFeeds[category];

                try
                {
                    var feed = await Task.Run(async () =>
                    {
                        using var client = new HttpClient();
                        var stream = await client.GetStreamAsync(feedUrl).ConfigureAwait(false);
                        using var reader = XmlReader.Create(stream);
                        return SyndicationFeed.Load(reader);
                    }).ConfigureAwait(false);

                    foreach (var item in feed.Items.Take(10))
                    {
                        articles.Add(new Article
                        {
                            Title = item.Title.Text,
                            Summary = item.Summary?.Text,
                            Url = item.Links.FirstOrDefault()?.Uri.ToString(),
                            Category = category,
                            Source = "NYTimes RSS",
                            PublishDate = item.PublishDate.DateTime
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"RSS error for {category}: {ex.Message}");
                }
            }

            return articles;
        }
    }
}