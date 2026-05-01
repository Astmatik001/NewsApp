using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NewsApp.Services
{
    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }
        
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class ChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }
        
        [JsonPropertyName("messages")]
        public ChatMessage[] Messages { get; set; }
        
        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    public class GrammarAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly string _model;

        public GrammarAnalysisService(string apiKey, string endpoint = "https://openrouter.ai/api/v1/chat/completions", string model = "openrouter/free")
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _endpoint = endpoint;
            _model = model;
        }

        public async Task<string> AnalyzeGrammarAsync(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return "[пусто]";

                var request = new ChatRequest
                {
                    Model = _model,
                    Messages = new[]
                    {
                        new ChatMessage 
                        { 
                            Role = "system", 
                            Content = """
                            Ты помощник, который делает синтаксический разбор английского текста для изучающих язык. 
                            Отвечай на русском, кратко и по делу. 
                            Не используй дополнительные символы для форматирования ответа.
                            """
                        },
                        new ChatMessage 
                        { 
                            Role = "user", 
                            Content = $"Сделай краткий грамматический разбор текста: \"{text}\"" 
                        }
                    },
                    Stream = false
                };

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(request, options);
                
                System.Diagnostics.Debug.WriteLine($"OpenRouter request JSON: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                var response = await _httpClient.PostAsync(_endpoint, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"OpenRouter response: {responseJson}");

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("choices", out var choices) &&
                        choices.GetArrayLength() > 0)
                    {
                        var message = choices[0].GetProperty("message");
                        var contentText = message.GetProperty("content").GetString();
                        return string.IsNullOrEmpty(contentText) ? "[пустой ответ]" : contentText.Trim();
                    }

                    return "[ошибка: неверный формат ответа]";
                }

                return $"[ошибка API: {response.StatusCode} - {responseJson}]";
            }
            catch (TaskCanceledException)
            {
                return "[ошибка: превышен таймаут запроса]";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GrammarAnalysis error: {ex.Message}");
                return $"[ошибка: {ex.Message}]";
            }
        }
    }
}
