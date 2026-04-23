using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewsApp.Services;
using Plugin.Maui.Audio;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        public ArticleDetailViewModel(
            LocalDatabaseService db,
            DeepSeekService deepSeek,
            CambAiTtsService tts,
            AnalyticsService analytics,
            IAudioManager audioManager)
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
                using var client = new System.Net.Http.HttpClient();
                var html = await client.GetStringAsync(url);
                ArticleHtmlContent = html;
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
                var plainText = Regex.Replace(ArticleHtmlContent, "<.*?>", string.Empty);
                var audioStream = await _tts.SynthesizeSpeechAsync(plainText);
                _currentPlayer = _audioManager.CreatePlayer(audioStream);
                _currentPlayer.Play();
                _currentPlayer.PlaybackEnded += (s, e) =>
                {
                    IsAudioPlaying = false;
                    _currentPlayer?.Dispose();
                    _currentPlayer = null;
                };
                await _analytics.TrackEventAsync("audio_played");
            }
            catch (System.Exception ex)
            {
                IsAudioPlaying = false;
                await Shell.Current.DisplayAlert("Error", $"Failed to play audio: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task AnalyzeGrammar()
        {
            if (IsLoadingAnalysis) return;
            IsLoadingAnalysis = true;
            var plainText = Regex.Replace(ArticleHtmlContent, "<.*?>", string.Empty);
            var result = await _deepSeek.AnalyzeGrammarAndVocabularyAsync(plainText);
            AnalysisResult = result;
            IsLoadingAnalysis = false;
            await _analytics.TrackEventAsync("grammar_analysis");
        }

        public async Task OnWordTapped(string word, string context)
        {
            var translation = await _deepSeek.TranslateWordAsync(word, context);
            await Shell.Current.DisplayAlert("Translation", $"{word} → {translation}", "OK");
            await _analytics.TrackEventAsync("word_tap", new() { { "word", word } });
        }
    }
}