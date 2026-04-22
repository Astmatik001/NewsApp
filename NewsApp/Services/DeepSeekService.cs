using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NewsApp.Services
{
    public class DeepSeekService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public DeepSeekService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        private async Task<string> SendPromptAsync(string systemPrompt, string userPrompt)
        {
            var requestBody = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("https://api.deepseek.com/v1/chat/completions", content);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }

        public async Task<string> TranslateWordAsync(string word, string contextSentence)
        {
            string system = "You are an English-to-Russian translator for a language learning app. Return ONLY the Russian translation of the given English word, considering context. No extra text.";
            string user = $"Word: {word}\nContext: {contextSentence}";
            return await SendPromptAsync(system, user);
        }

        public async Task<string> AnalyzeGrammarAndVocabularyAsync(string articleText)
        {
            string system = @"You are an English teacher for Russian students. Analyze the given news article. Output JSON with:
- grammar_points: array of complex structures (e.g., passive voice, conditionals) with examples from article.
- vocabulary: array of 10 important words/phrases, each with English definition and Russian translation.
Keep at B2-C1 level.";
            return await SendPromptAsync(system, articleText);
        }
    }
}