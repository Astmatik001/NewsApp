using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using NewsApp.Models;

namespace NewsApp.Services
{
    public class NyTimesService : INewsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private string _accessToken;
        private DateTime _tokenExpiry;

        public NyTimesService(string apiKey, string apiSecret)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _httpClient = new HttpClient();
        }

        private async Task<string> GetAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiry > DateTime.UtcNow)
                return _accessToken;

            var tokenUrl = "https://developer.nytimes.com/auth/oauth/v2/token";
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _apiKey,
                ["client_secret"] = _apiSecret
            });

            var response = await _httpClient.PostAsync(tokenUrl, content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            _accessToken = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn);
            return _accessToken;
        }

        public async Task<List<Article>> GetHeadlinesAsync(List<string> categories)
        {
            var token = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var articles = new List<Article>();
            foreach (var category in categories)
            {
                var url = $"https://api.nytimes.com/svc/topstories/v2/{category}.json";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var results = doc.RootElement.GetProperty("results");
                foreach (var item in results.EnumerateArray())
                {
                    articles.Add(new Article
                    {
                        Title = item.GetProperty("title").GetString(),
                        Summary = item.GetProperty("abstract").GetString(),
                        Url = item.GetProperty("url").GetString(),
                        Category = category,
                        Source = "NYTimes",
                        PublishDate = DateTime.Parse(item.GetProperty("published_date").GetString())
                    });
                }
            }
            return articles;
        }
    }
}