using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using NewsApp.Models;
using NewsApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
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

        public ArticleDetailPage()
        {
            InitializeComponent();
            OpenUrlButton.Clicked += OnOpenUrlClicked;
            TranslateSelectedButton.Clicked += OnTranslateSelectedClicked;

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
                var article = new Article
                {
                    Title = ArticleTitle,
                    Summary = _summary,
                    Source = SourceLabel.Text,
                    Url = ArticleUrl
                };
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
                var article = new Article
                {
                    Title = ArticleTitle,
                    Summary = _summary,
                    Source = SourceLabel.Text,
                    Url = ArticleUrl
                };
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
                // Show loading placeholder
                ContentWebView.Source = new HtmlWebViewSource
                {
                    Html = "<html><body style='padding:20px'><p>Загрузка...</p></body></html>"
                };

                var content = await Task.Run(() => FetchArticleContentAsync(url));
                string displayContent;
                if (!string.IsNullOrEmpty(content))
                {
                    var cleanContent = System.Text.RegularExpressions.Regex.Replace(content,
                        @"<script.*?</script>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    cleanContent = System.Text.RegularExpressions.Regex.Replace(cleanContent,
                        @"<style.*?</style>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    displayContent = cleanContent;
                }
                else
                {
                    displayContent = $@"
                        <div style='color: #666; font-style: italic;'>
                            {System.Security.SecurityElement.Escape(_summary ?? "Нет описания")}
                        </div>
                        <div style='color: #999; font-size: 14px; margin-top: 10px;'>
                            Полный текст недоступен
                        </div>";
                }

                var htmlContent = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=yes'>
    <style>
        body {{
            padding: 15px;
            font-family: -apple-system, system-ui, sans-serif;
            font-size: 16px;
            line-height: 1.6;
            color: #333;
            -webkit-user-select: text;
            user-select: text;
            background: white;
        }}
        p {{ margin-bottom: 1em; }}
        a {{ pointer-events: none; color: inherit; text-decoration: none; }}
    </style>
</head>
<body>
    {displayContent}
</body>
</html>";
                // Set the source on UI thread
                ContentWebView.Source = new HtmlWebViewSource { Html = htmlContent };
            }
            catch (Exception ex)
            {
                ContentWebView.Source = new HtmlWebViewSource
                {
                    Html = $"<html><body style='padding:20px'><p style='color:red'>Ошибка: {ex.Message}</p></body></html>"
                };
            }
        }

        private async Task<string> FetchArticleContentAsync(string url)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var html = await client.GetStringAsync(url);

                const string startTag = "<p";
                const string endTag = "</p>";
                var content = new System.Text.StringBuilder();
                var index = 0;

                while (true)
                {
                    index = html.IndexOf(startTag, index);
                    if (index < 0) break;
                    var closeIndex = html.IndexOf(endTag, index);
                    if (closeIndex < 0) break;
                    var para = html.Substring(index, closeIndex - index + 4);
                    if (para.Contains("<p") && !para.Contains("class="))
                    {
                        var textStart = para.IndexOf(">", startTag.Length);
                        if (textStart > 0)
                        {
                            var text = para.Substring(textStart + 1);
                            if (text.Length > 20 && !text.Contains("<"))
                                content.AppendLine(text.Trim());
                        }
                    }
                    index = closeIndex + 1;
                }
                var result = content.ToString().Trim();
                return string.IsNullOrEmpty(result) ? null : result;
            }
            catch { return null; }
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

                var js = "window.getSelection().toString().trim();";
                var selectedText = await ContentWebView.EvaluateJavaScriptAsync(js);
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

        // No navigation interfering – we don't cancel anything
        private void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
        {
            // Do nothing – allow all navigation. Links are disabled via CSS anyway.
        }

        private void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Optional: log if needed
        }

        private async void OnOpenUrlClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(ArticleUrl))
                await Launcher.OpenAsync(ArticleUrl);
        }
    }
}