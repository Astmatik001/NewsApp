using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NewsApp.Services
{
    public class TranslationService
    {
        private readonly HttpClient _httpClient;

        public TranslationService(string apiUrl)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public async Task<string> TranslateWordAsync(string word)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(word))
                    return "[пусто]";

                // MyMemory API – бесплатно, без ключа
                string url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(word)}&langpair=en|ru";
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                string responseJson = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine($"MyMemory response: {responseJson}");

                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument doc = JsonDocument.Parse(responseJson);
                    string translatedText = doc.RootElement
                        .GetProperty("responseData")
                        .GetProperty("translatedText")
                        .GetString();

                    // Удаляем возможные HTML теги из ответа
                    if (!string.IsNullOrEmpty(translatedText))
                    {
                        translatedText = System.Text.RegularExpressions.Regex.Replace(translatedText, "<[^>]*>", "");

                        if (translatedText != word)
                        {
                            return translatedText;
                        }
                        return "[перевод не найден]";
                    }
                }

                return $"[ошибка {response.StatusCode}]";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Translation error: {ex.Message}");
                return $"[ошибка: {ex.Message}]";
            }
        }
    }
}