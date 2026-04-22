using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NewsApp.Services
{
    public class CambAiTtsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string TtsStreamEndpoint = "https://client.camb.ai/apis/tts-stream";

        public CambAiTtsService(string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        }

        /// <summary>
        /// Generates speech from text using Camb AI's streaming endpoint.
        /// Returns an audio stream (MP3 format by default) that can be played immediately.
        /// </summary>
        /// <param name="text">The text to convert to speech (max ~5000 characters)</param>
        /// <param name="voiceId">Voice ID (147320 = Gary, a warm English voice)</param>
        /// <param name="language">Language code (en-us, en-gb, es-es, etc.)</param>
        /// <param name="speechModel">Model: mars-flash (fastest), mars-pro (high quality), mars-instruct (emotion control)</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        public async Task<Stream> SynthesizeSpeechAsync(
            string text,
            int voiceId = 147320,
            string language = "en-us",
            string speechModel = "mars-flash",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text cannot be empty", nameof(text));

            var payload = new
            {
                text = text,
                voice_id = voiceId,
                language = language,
                speech_model = speechModel,
                output_configuration = new { format = "mp3" }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _httpClient.PostAsync(TtsStreamEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }

        public async Task SaveSpeechToFileAsync(
            string text,
            string filePath,
            int voiceId = 147320,
            string language = "en-us",
            string speechModel = "mars-flash",
            CancellationToken cancellationToken = default)
        {
            using var audioStream = await SynthesizeSpeechAsync(text, voiceId, language, speechModel, cancellationToken);
            using var fileStream = File.Create(filePath);
            await audioStream.CopyToAsync(fileStream, cancellationToken);
        }

        public async Task<List<CambAiVoice>> ListVoicesAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync("https://client.camb.ai/apis/list-voices", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var voices = JsonSerializer.Deserialize<List<CambAiVoice>>(json);
            return voices ?? new List<CambAiVoice>();
        }
    }

    // Model for voice information
    public class CambAiVoice
    {
        public int Id { get; set; }
        public string VoiceName { get; set; }
        public int Gender { get; set; } // 1 = male, 2 = female
        public int Age { get; set; }
        public string Description { get; set; }
        public bool IsPublished { get; set; }
        public int? Language { get; set; }
    }
}