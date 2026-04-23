using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewsApp.Services;
using Plugin.Maui.Audio;
using SmartReader;
using System;
using System.IO;
using System.Reflection.PortableExecutable;
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
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    ArticleTitle = "Error";
                    ArticleHtmlContent = "<html><body><p>Invalid article URL.</p></body></html>";
                    return;
                }

                // Try cache
                var cached = await _db.GetCachedArticleByUrlAsync(url);
                if (cached != null && !string.IsNullOrEmpty(cached.ContentHtml))
                {
                    ArticleTitle = cached.Title ?? "Article";
                    ArticleHtmlContent = cached.ContentHtml;
                    await _analytics.TrackEventAsync("article_view", new() { { "url", url }, { "source", "cache" } });
                    return;
                }

                // Fetch with SmartReader
                var article = await ExtractArticleFromUrlAsync(url);
                if (article != null && article.IsReadable)
                {
                    string fullHtml = BuildHtmlContent(article);
                    ArticleHtmlContent = fullHtml;
                    ArticleTitle = article.Title ?? "Article";

                    // Save to cache
                    if (cached != null)
                    {
                        cached.ContentHtml = fullHtml;
                        await _db.CacheArticleAsync(cached);
                    }
                    else
                    {
                        var newArticle = new Models.Article
                        {
                            Url = url,
                            Title = article.Title,
                            ContentHtml = fullHtml,
                            Summary = article.Excerpt ?? "",
                            Source = "NYTimes RSS",
                            PublishDate = article.PublicationDate ?? DateTime.UtcNow
                        };
                        await _db.CacheArticleAsync(newArticle);
                    }
                    await _analytics.TrackEventAsync("article_view", new() { { "url", url }, { "source", "smartreader" } });
                }
                else
                {
                    await FallbackToRawHtml(url, cached);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadArticle exception: {ex}");
                // Display error in article page
                ArticleTitle = "Error loading article";
                ArticleHtmlContent = $"<html><body><p>Failed to load article: {ex.Message}</p></body></html>";
                await _analytics.TrackEventAsync("article_load_error", new() { { "url", url }, { "error", ex.Message } });
            }
        }

        private string BuildHtmlContent(Article article)
        {
            string title = System.Security.SecurityElement.Escape(article.Title ?? "No title");
            string date = article.PublicationDate?.ToString("dd MMM yyyy") ?? "Date unknown";
            string content = article.Content ?? "<p>Content not available.</p>";

            return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=yes'>
            <style>
                body {{
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
                    font-size: 18px;
                    line-height: 1.6;
                    padding: 20px;
                    color: #333;
                }}
                h1 {{
                    font-size: 28px;
                    margin-bottom: 16px;
                }}
                p {{
                    margin-bottom: 1em;
                }}
                img {{
                    max-width: 100%;
                    height: auto;
                }}
                .published-date {{
                    color: #666;
                    font-size: 14px;
                    margin-bottom: 20px;
                }}
            </style>
        </head>
        <body>
            <h1>{title}</h1>
            <div class='published-date'>{date}</div>
            {content}
        </body>
        </html>";
        }

        private async Task FallbackToRawHtml(string url, Models.Article cached)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                var html = await client.GetStringAsync(url);
                ArticleHtmlContent = html;
                ArticleTitle = cached?.Title ?? ExtractTitleFromHtml(html) ?? "Article";
                await _analytics.TrackEventAsync("article_view", new() { { "url", url }, { "source", "raw_html" } });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Raw HTML fallback error: {ex.Message}");
                var fallbackHtml = cached?.Summary ?? "Content could not be loaded.";
                ArticleHtmlContent = $"<html><body><p>{fallbackHtml}</p></body></html>";
                ArticleTitle = cached?.Title ?? "Article";
                await _analytics.TrackEventAsync("article_view", new() { { "url", url }, { "source", "error_fallback" } });
            }
        }

        private async Task<Article?> ExtractArticleFromUrlAsync(string url)
        {
            try
            {
                var reader = new Reader(url);
                return await reader.GetArticleAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SmartReader extraction error: {ex.Message}");
                return null;
            }
        }

        private string ExtractTitleFromHtml(string html)
        {
            var match = Regex.Match(html, @"<title>(.*?)</title>", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : null;
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
                if (string.IsNullOrWhiteSpace(plainText))
                    plainText = ArticleTitle;

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
            catch (Exception ex)
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