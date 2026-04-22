using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewsApp.Services;
using System.IO;
using System.Threading.Tasks;
using Plugin.Maui.Audio;

namespace NewsApp.ViewModels
{
    public partial class ArticleDetailViewModel : ObservableObject
    {
        private readonly LocalDatabaseService _db;
        private readonly DeepSeekService _deepSeek;
        private readonly CambAiTtsService _tts;
        private readonly AnalyticsService _analytics;
        private readonly IAudioManager _audioManager;
        private IAudioPlayer _currentPlayer;

        [ObservableProperty]
        private string articleTitle;

        [ObservableProperty]
        private string articleHtmlContent;

        [ObservableProperty]
        private bool isAudioPlaying;

        [ObservableProperty]
        private bool isLoadingAnalysis;

        [ObservableProperty]
        private string analysisResult;

        public ArticleDetailViewModel(LocalDatabaseService db, DeepSeekService deepSeek, CambAiTtsService tts, AnalyticsService analytics, IAudioManager audioManager)
        {
            _db = db;
            _deepSeek = deepSeek;
            _tts = tts;
            _analytics = analytics;
            _audioManager = audioManager;
        }

        public async Task LoadArticle(string url)
        {
            var cached = await _db.GetCachedArticleByUrlAsync(url);
            if (cached != null && !string.IsNullOrEmpty(cached.ContentHtml))
            {
                ArticleTitle = cached.Title;
                ArticleHtmlContent = cached.ContentHtml;
            }
            else
            {
                // Fetch full article from original URL (simplified: load raw HTML)
                using var client = new System.Net.Http.HttpClient();
                var html = await client.GetStringAsync(url);
                ArticleHtmlContent = html; // In production, extract main content
                ArticleTitle = cached?.Title ?? "Article";
                if (cached != null)
                {
                    cached.ContentHtml = html;
                    await _db.CacheArticleAsync(cached);
                }
            }
            await _analytics.TrackEventAsync("article_view", new() { { "url", url } });
        }

        [RelayCommand]
        private async Task PlayAudio()
        {
            if (_currentPlayer?.IsPlaying == true)
            {
                _currentPlayer.Stop();
                IsAudioPlaying = false;
                return;
            }

            try
            {
                IsAudioPlaying = true;

                // Extract plain text from HTML content
                var plainText = System.Text.RegularExpressions.Regex.Replace(
                    ArticleHtmlContent,
                    "<.*?>",
                    string.Empty
                );

                // Generate speech stream from Camb AI
                var audioStream = await _tts.SynthesizeSpeechAsync(
                    text: plainText,
                    voiceId: 147320,      // "Gary" - warm English voice
                    language: "en-us",
                    speechModel: "mars-flash"
                );

                // Create and play audio player
                _currentPlayer = _audioManager.CreatePlayer(audioStream);
                _currentPlayer.Play();

                // Handle playback end
                _currentPlayer.PlaybackEnded += (s, e) =>
                {
                    IsAudioPlaying = false;
                    _currentPlayer?.Dispose();
                    _currentPlayer = null;
                };

                await _analytics.TrackEventAsync("audio_played");
            }
            catch (Exception ex)
            {
                IsAudioPlaying = false;
                await Shell.Current.DisplayAlert("Error", $"Failed to generate audio: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task AnalyzeGrammar()
        {
            if (IsLoadingAnalysis) return;
            IsLoadingAnalysis = true;
            var plainText = System.Text.RegularExpressions.Regex.Replace(ArticleHtmlContent, "<.*?>", string.Empty);
            var result = await _deepSeek.AnalyzeGrammarAndVocabularyAsync(plainText);
            AnalysisResult = result;
            IsLoadingAnalysis = false;
            await _analytics.TrackEventAsync("grammar_analysis");
        }

        // Called from WebView when a word is tapped
        public async Task OnWordTapped(string word, string context)
        {
            var translation = await _deepSeek.TranslateWordAsync(word, context);
            // Show popup – implement using Application.Current.MainPage.DisplayAlert
            await Shell.Current.DisplayAlert("Translation", $"{word} → {translation}", "OK");
            await _analytics.TrackEventAsync("word_tap", new() { { "word", word } });
        }
    }
}