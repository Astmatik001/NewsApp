using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using NewsApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Plugin.Maui.Audio;
using SmartReader;
using Article = NewsApp.Models.Article;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NewsApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ArticleDetailPage : ContentPage
    {
        public string ArticleUrl { get; set; }
        public string ArticleTitle { get; set; }
        private string _summary;
        private bool _isUpdating = false;
        private IAudioPlayer? _audioPlayer;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private bool _isProcessing = false;
        private string _fullText = "";

        public ArticleDetailPage()
        {
            InitializeComponent();
            Disappearing += OnPageDisappearing;

            ReadCheckBox.CheckedChanged += async (s, e) => {
                if (_isUpdating) return;
                _isUpdating = true;
                ReadLabel.Text = ReadCheckBox.IsChecked ? "Прочитано ✓" : "Прочитано";
                ReadLabel.TextColor = ReadCheckBox.IsChecked ? Colors.Green : Colors.Gray;
                if (ReadCheckBox.IsChecked && !string.IsNullOrEmpty(ArticleUrl))
                    await SaveReadStatus();
                _isUpdating = false;
            };

            FavoriteCheckBox.CheckedChanged += async (s, e) => {
                if (_isUpdating) return;
                _isUpdating = true;
                FavoriteLabel.Text = FavoriteCheckBox.IsChecked ? "В избранном ★" : "В избранное";
                FavoriteLabel.TextColor = FavoriteCheckBox.IsChecked ? Colors.Orange : Colors.Gray;
                if (FavoriteCheckBox.IsChecked && !string.IsNullOrEmpty(ArticleUrl))
                    await SaveFavoriteStatus();
                _isUpdating = false;
            };
        }

        private void OnReadTapped(object sender, EventArgs e) => ReadCheckBox.IsChecked = !ReadCheckBox.IsChecked;
        private void OnFavoriteTapped(object sender, EventArgs e) => FavoriteCheckBox.IsChecked = !FavoriteCheckBox.IsChecked;

        private async Task SaveReadStatus()
        {
            try
            {
                var userId = Preferences.Get("user_id", "");
                if (App.ServiceProvider == null || string.IsNullOrEmpty(userId)) return;
                var db = App.ServiceProvider.GetRequiredService<LocalDatabaseService>();
                var article = new Article { Title = ArticleTitle, Summary = _summary, Source = SourceLabel.Text, Url = ArticleUrl };
                await db.MarkAsReadAsync(userId, article);
            }
            catch { }
        }

        private async Task SaveFavoriteStatus()
        {
            try
            {
                var userId = Preferences.Get("user_id", "");
                if (App.ServiceProvider == null || string.IsNullOrEmpty(userId)) return;
                var db = App.ServiceProvider.GetRequiredService<LocalDatabaseService>();
                var article = new Article { Title = ArticleTitle, Summary = _summary, Source = SourceLabel.Text, Url = ArticleUrl };
                await db.MarkAsFavoriteAsync(userId, article);
            }
            catch { }
        }

        public ArticleDetailPage(string title, string summary, string source, string url) : this()
        {
            Title = title;
            ArticleTitle = title;
            ArticleUrl = url;
            _summary = summary;

            TitleLabel.Text = title;
            SourceLabel.Text = source;

            LoadContent(url);
        }

        private async void LoadContent(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            try
            {
                ContentWebView.Source = new HtmlWebViewSource
                {
                    Html = "<html><body style='padding:20px;font-family:system-ui'><p>⏳ Загрузка...</p></body></html>"
                };

                var (htmlBody, plainText) = await Task.Run(() => FetchArticleContentAsync(url));

                _fullText = plainText;

                string displayContent = !string.IsNullOrWhiteSpace(htmlBody)
                    ? htmlBody
                    : $@"<p style='color:#666;font-style:italic'>{System.Security.SecurityElement.Escape(_summary ?? "Нет описания")}</p>
                         <p style='color:#999;font-size:14px'>Полный текст недоступен — откройте статью на сайте.</p>";

                var fullHtml = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=yes'>
    <style>
        body {{
            padding: 15px;
            font-family: -apple-system, system-ui, sans-serif;
            font-size: 16px;
            line-height: 1.7;
            color: #222;
            background: white;
            -webkit-user-select: text;
            user-select: text;
        }}
        p  {{ margin-bottom: 1em; }}
        h2, h3 {{ color: #1A1A2E; margin-top: 1.4em; }}
        img {{ max-width: 100%; height: auto; border-radius: 6px; }}
        a  {{ pointer-events: none; color: inherit; text-decoration: none; }}
        blockquote {{ border-left: 3px solid #1976D2; margin: 1em 0; padding: 6px 14px; color: #555; font-style: italic; }}
        figure {{ margin: 1em 0; }}
        figcaption {{ font-size: 13px; color: #888; }}
    </style>
</head>
<body>
    {displayContent}
</body>
</html>";

                ContentWebView.Source = new HtmlWebViewSource { Html = fullHtml };
            }
            catch (Exception ex)
            {
                ContentWebView.Source = new HtmlWebViewSource
                {
                    Html = $"<html><body style='padding:20px'><p style='color:red'>Ошибка: {ex.Message}</p></body></html>"
                };
            }
        }

        /// <summary>
        /// Returns (htmlBody, plainText).
        /// Strategy: 1) SmartReader  2) CSS-selector paragraph extraction  3) All &lt;p&gt; tags
        /// </summary>
        private async Task<(string html, string plain)> FetchArticleContentAsync(string url)
        {
            // 1. SmartReader
            try
            {
                var reader = new Reader(url);
                var article = await reader.GetArticleAsync();
                if (article != null && article.IsReadable && !string.IsNullOrWhiteSpace(article.Content))
                {
                    var plain = HtmlToPlain(article.Content);
                    if (plain.Length > 200)
                        return (article.Content, plain);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SmartReader: {ex.Message}");
            }

            // 2. Raw HTML extraction
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");

                var raw = await client.GetStringAsync(url);

                // Remove scripts/styles first
                raw = Regex.Replace(raw, @"<(script|style)[^>]*>[\s\S]*?</\1>", "", RegexOptions.IgnoreCase);

                // Try article containers
                var containerPatterns = new[]
                {
                    @"<article[^>]*>([\s\S]*?)</article>",
                    @"<(?:div|section)[^>]+class=""[^""]*(?:article-body|articleBody|story-body|StoryBodyCompanion|post-content|entry-content|article-content|article__body|body-content|ArticleBody)[^""]*""[^>]*>([\s\S]*?)</(?:div|section)>",
                    @"<section[^>]+name=""articleBody""[^>]*>([\s\S]*?)</section>",
                    @"<main[^>]*>([\s\S]*?)</main>",
                };

                foreach (var pattern in containerPatterns)
                {
                    var m = Regex.Match(raw, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (!m.Success) continue;

                    var inner = m.Groups[1].Value;
                    var paras = Regex.Matches(inner, @"<p[^>]*>([\s\S]*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline)
                        .Cast<Match>()
                        .Select(p => p.Value)
                        .Where(p => HtmlToPlain(p).Length > 30)
                        .ToList();

                    if (paras.Count >= 3)
                    {
                        var html = string.Join("\n", paras);
                        var plain = HtmlToPlain(html);
                        if (plain.Length > 200)
                            return (html, plain);
                    }
                }

                // Fallback: all <p> from page
                var allParas = Regex.Matches(raw, @"<p[^>]*>([\s\S]*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline)
                    .Cast<Match>()
                    .Select(p => p.Value)
                    .Where(p => HtmlToPlain(p).Length > 60)
                    .Take(60)
                    .ToList();

                if (allParas.Count >= 3)
                {
                    var html = string.Join("\n", allParas);
                    var plain = HtmlToPlain(html);
                    if (plain.Length > 200)
                        return (html, plain);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RawHtml: {ex.Message}");
            }

            return (null, "");
        }

        private static string HtmlToPlain(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";
            
            // Remove markdown images ![alt](url)
            html = Regex.Replace(html, @"!\[[^\]]*\]\([^)]+\)", "", RegexOptions.IgnoreCase);
            
            // Remove HTML tags
            html = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"</(p|div|h[1-6]|li|blockquote|tr)[^>]*>", "\n\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<[^>]+>", "");
            
            // Decode HTML entities
            html = System.Net.WebUtility.HtmlDecode(html);
            
            // Remove URLs
            html = Regex.Replace(html, @"https?://[^\s\]]+", "", RegexOptions.IgnoreCase);
            
            // Remove control characters except newlines
            html = Regex.Replace(html, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
            
            // Normalize whitespace
            html = Regex.Replace(html, @"\n{3,}", "\n\n");
            html = Regex.Replace(html, @"[ \t]{2,}", " ");
            
            return html.Trim();
        }

        private static string DetectLanguage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "en-us";
            
            // Count Russian vs English characters
            int russianChars = text.Count(c => (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я') || c == 'ё' || c == 'Ё');
            int totalLetters = text.Count(c => char.IsLetter(c));
            
            if (totalLetters == 0)
                return "en-us";
            
            double russianRatio = (double)russianChars / totalLetters;
            return russianRatio > 0.3 ? "ru-ru" : "en-us";
        }

        private static string CleanTextForTts(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            
            // Remove markdown images
            text = Regex.Replace(text, @"!\[[^\]]*\]\([^)]+\)", "", RegexOptions.IgnoreCase);
            
            // Remove URLs
            text = Regex.Replace(text, @"https?://[^\s\]]+", "", RegexOptions.IgnoreCase);
            
            // Replace newlines with spaces for TTS
            text = Regex.Replace(text, @"\s+", " ");
            
            // Remove control characters
            text = Regex.Replace(text, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
            
            // Limit length (Camb AI limit ~5000 chars)
            if (text.Length > 500)
                text = text.Substring(0, 495) + "...";
            
            return text.Trim();
        }

        private async void OnTranslateSelectedClicked(object sender, EventArgs e)
        {
            try
            {
                if (ContentWebView == null || !ContentWebView.IsLoaded)
                {
                    await DisplayAlert("Информация", "Подождите, страница ещё загружается", "OK");
                    return;
                }

                var selectedText = await ContentWebView.EvaluateJavaScriptAsync("window.getSelection().toString().trim();");
                if (!string.IsNullOrEmpty(selectedText))
                {
                    var translationService = App.ServiceProvider?.GetRequiredService<TranslationService>();
                    if (translationService != null)
                    {
                        var translation = await translationService.TranslateWordAsync(selectedText);
                        await DisplayAlert("Перевод", $"{selectedText}\n\n↓\n{translation}", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Ошибка", "Сервис перевода не найден", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Информация", "Выделите слово перед нажатием кнопки", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void OnTtsClicked(object sender, EventArgs e)
        {
            if (_isProcessing) return;

            if (_isPaused)
            {
                _audioPlayer?.Play();
                _isPaused = false;
                _isPlaying = true;
                UpdateButtons();
                return;
            }

            if (_isPlaying)
            {
                _audioPlayer?.Pause();
                _isPlaying = false;
                _isPaused = true;
                UpdateButtons();
                return;
            }

            if (TtsButton.Text.Contains("Загрузка")) return;

            _isProcessing = true;
            TtsButton.IsEnabled = false;
            TtsButton.Text = "⏳ Загрузка...";
            StopButton.IsEnabled = false;

            var textToSpeak = string.IsNullOrEmpty(_fullText) ? _summary : _fullText;
            textToSpeak = CleanTextForTts(textToSpeak);

            if (string.IsNullOrEmpty(textToSpeak))
            {
                await DisplayAlert("Информация", "Нет текста для озвучки", "OK");
                _isProcessing = false;
                UpdateButtons();
                return;
            }

            try
            {
                var ttsService = App.ServiceProvider?.GetRequiredService<CambAiTtsService>();
                if (ttsService == null)
                {
                    await DisplayAlert("Ошибка", "Сервис озвучки не найден", "OK");
                    _isProcessing = false;
                    UpdateButtons();
                    return;
                }

                // Auto-detect language based on text content
                var language = DetectLanguage(textToSpeak);
                
                var audioStream = await ttsService.SynthesizeSpeechAsync(textToSpeak, language: language);
                if (audioStream == null)
                {
                    await DisplayAlert("Ошибка", "Не удалось получить аудио от сервиса озвучки", "OK");
                    _isProcessing = false;
                    UpdateButtons();
                    return;
                }

                var tempFile = Path.Combine(Path.GetTempPath(), $"tts_{Guid.NewGuid()}.mp3");
                using (var fileStream = File.Create(tempFile))
                    await audioStream.CopyToAsync(fileStream);

                var audioManager = App.ServiceProvider?.GetRequiredService<IAudioManager>();
                if (audioManager == null)
                {
                    await DisplayAlert("Ошибка", "Сервис аудио не найден.", "OK");
                    _isProcessing = false;
                    UpdateButtons();
                    return;
                }

                _audioPlayer = audioManager.CreatePlayer(tempFile);
                if (_audioPlayer == null)
                {
                    await DisplayAlert("Ошибка", "Не удалось создать аудио плеер", "OK");
                    _isProcessing = false;
                    UpdateButtons();
                    return;
                }

                _audioPlayer.PlaybackEnded += (s, args) => {
                    _isPlaying = false;
                    _isPaused = false;
                    _isProcessing = false;
                    StopAudio(fullStop: true);
                    try { File.Delete(tempFile); } catch { }
                };

                _audioPlayer.Play();
                _isProcessing = false;
                _isPlaying = true;
                _isPaused = false;
                UpdateButtons();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TTS] Error: {ex}");
                await DisplayAlert("Ошибка", ex.Message, "OK");
                _isProcessing = false;
                UpdateButtons();
            }
        }

        private void OnStopClicked(object sender, EventArgs e)
        {
            if (_isPlaying || _isPaused)
                StopAudio(fullStop: true);
        }

        private void OnWebViewNavigating(object sender, WebNavigatingEventArgs e) { }
        private void OnWebViewNavigated(object sender, WebNavigatedEventArgs e) { }

        private async void OnOpenUrlClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(ArticleUrl))
                await Launcher.OpenAsync(ArticleUrl);
        }

        private void OnPageDisappearing(object sender, EventArgs e) => StopAudio(fullStop: true);

        private void StopAudio(bool fullStop = false)
        {
            _isPlaying = false;
            _isPaused = false;
            if (fullStop) _isProcessing = false;

            if (_audioPlayer != null)
            {
                try { if (_audioPlayer.IsPlaying) _audioPlayer.Stop(); } catch { }
                try { _audioPlayer.Dispose(); } catch { }
                _audioPlayer = null;
            }
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            if (TtsButton != null)
            {
                TtsButton.Text = _isPlaying ? "⏸ Пауза" : "▶ Воспроизведение";
                TtsButton.IsEnabled = true;
            }

            if (StopButton != null)
            {
                StopButton.IsEnabled = _isPlaying || _isPaused;
                StopButton.BackgroundColor = (_isPlaying || _isPaused)
                    ? Color.FromArgb("#D32F2F")
                    : Color.FromArgb("#757575");
            }
        }
    }
}
