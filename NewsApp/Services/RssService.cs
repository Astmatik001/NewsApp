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
            { "World", "http://feeds.bbci.co.uk/news/world/rss.xml" },
            { "Technology", "http://feeds.bbci.co.uk/news/technology/rss.xml" },
            { "Business", "http://feeds.bbci.co.uk/news/business/rss.xml" },
            { "Science", "http://feeds.bbci.co.uk/news/science_and_environment/rss.xml" }
        };

        public async Task<List<Article>> GetHeadlinesAsync(List<string> categories)
        {
            var articles = new List<Article>();
            foreach (var category in categories)
            {
                if (!_rssFeeds.ContainsKey(category)) continue;
                var feedUrl = _rssFeeds[category];
                using var client = new HttpClient();
                var stream = await client.GetStreamAsync(feedUrl);
                using var reader = XmlReader.Create(stream);
                var feed = SyndicationFeed.Load(reader);
                foreach (var item in feed.Items.Take(10))
                {
                    articles.Add(new Article
                    {
                        Title = item.Title.Text,
                        Summary = item.Summary?.Text,
                        Url = item.Links.FirstOrDefault()?.Uri.ToString(),
                        Category = category,
                        Source = "RSS",
                        PublishDate = item.PublishDate.DateTime,
                        ContentHtml = null
                    });
                }
            }
            return articles;
        }
    }
}