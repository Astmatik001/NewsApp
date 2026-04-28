using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;
using NewsApp.Models;
using NewsApp.Services;
using NewsApp.ViewModels;


namespace NewsApp.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ArticleDetailPage : ContentPage
    {
        public string ArticleUrl { get; set; }
        public string ArticleTitle { get; set; }

        public ArticleDetailPage()
        {
            InitializeComponent();
            OpenUrlButton.Clicked += OnOpenUrlClicked;
            TranslateSelectedButton.Clicked += OnTranslateSelectedClicked;

            ContentWebView.HandlerChanged += OnWebViewHandlerChanged;

            ReadCheckBox.CheckedChanged += async (s, e) =>
            {
                ReadLabel.Text = ReadCheckBox.IsChecked ? "Прочитано ✓" : "Прочитано";
                ReadLabel.TextColor = ReadCheckBox.IsChecked ? Colors.Green : Colors.Gray;
                if (ReadCheckBox.IsChecked && !string.IsNullOrEmpty(ArticleUrl))
                    await SaveReadStatus();
            };

            FavoriteCheckBox.CheckedChanged += async (s, e) =>
            {
                FavoriteLabel.Text = FavoriteCheckBox.IsChecked ? "В избранном ★" : "В избранное";
                FavoriteLabel.TextColor = FavoriteCheckBox.IsChecked ? Colors.Orange : Colors.Gray;
                if (FavoriteCheckBox.IsChecked && !string.IsNullOrEmpty(ArticleUrl))
                    await SaveFavoriteStatus();
            };
        }

        private void OnWebViewHandlerChanged(object sender, EventArgs e)
        {
            //SetupCustomWebViewClient();
        }

        private void SetupCustomWebViewClient()
        {
#if ANDROID
            if (ContentWebView.Handler?.PlatformView is Android.Webkit.WebView platformWebView)
            {
                System.Diagnostics.Debug.WriteLine("Custom WebViewClient установлен");
            }
#endif
        }

        public ArticleDetailPage(string title, string summary, string source, string url) : this()
        {
            ArticleTitle = title;
            ArticleUrl = url;
            TitleLabel.Text = title;
            SummaryLabel.Text = summary;
            SourceLabel.Text = source;

            LoadContent(url);
        }

        private async void LoadContent(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            try
            {
                var content = await FetchArticleContentAsync(url);

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
                <div style='color: #666; font-style: italic; padding: 10px;'>
                    {System.Security.SecurityElement.Escape(SummaryLabel.Text ?? "Нет описания")}
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
                            }}
                            p {{ margin-bottom: 1em; }}
                            a {{ pointer-events: none; color: inherit; text-decoration: none; }}
                        </style>
                    </head>
                    <body>
                        {displayContent}
                    </body>
                    </html>";

                // Сбрасываем флаг перед новой загрузкой
                _isInitialLoad = true;
                ContentWebView.Source = new HtmlWebViewSource { Html = htmlContent };
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка загрузки", ex.Message, "OK");
                // Показываем сообщение об ошибке в WebView
                var errorHtml = $"<html><body style='padding:20px'><p style='color:red'>Ошибка: {ex.Message}</p></body></html>";
                ContentWebView.Source = new HtmlWebViewSource { Html = errorHtml };
            }
#if ANDROID
            if (ContentWebView.Handler?.PlatformView is Android.Webkit.WebView platformWebView)
            {
                platformWebView.SetLayerType(Android.Views.LayerType.Software, null);
            }
#endif
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
            catch
            {
                return null;
            }
        }

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
                    Summary = SummaryLabel.Text,
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
                    Summary = SummaryLabel.Text,
                    Source = SourceLabel.Text,
                    Url = ArticleUrl
                };
                await db.MarkAsFavoriteAsync(userId, article);
            }
            catch { }
        }

        private async void OnOpenUrlClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(ArticleUrl))
                await Launcher.OpenAsync(ArticleUrl);
        }

        private async void OnTranslateSelectedClicked(object sender, EventArgs e)
        {
            try
            {
                var jsCode = "window.getSelection().toString().trim();";
                var selectedText = await ContentWebView.EvaluateJavaScriptAsync(jsCode);

                if (!string.IsNullOrEmpty(selectedText) && selectedText.Length < 100)
                {
                    TranslateSelectedButton.Text = "⏳ Перевод...";
                    TranslateSelectedButton.IsEnabled = false;

                    var translationService = App.ServiceProvider?.GetRequiredService<TranslationService>();
                    if (translationService != null)
                    {
                        var translation = await translationService.TranslateWordAsync(selectedText);
                        await DisplayAlert("Перевод", $"{selectedText}\n\n↓\n{translation}", "OK");
                    }
                }
                else if (selectedText.Length >= 100)
                {
                    await DisplayAlert("Информация", "Выделено слишком много текста (максимум 100 символов)", "OK");
                }
                else
                {
                    await DisplayAlert("Информация", "Выделите слово или фразу для перевода", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
            finally
            {
                TranslateSelectedButton.Text = "📖 Перевести выделенное слово";
                TranslateSelectedButton.IsEnabled = true;
            }
        }

        private bool _isInitialLoad = true;

        private void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
        {
            // Разрешаем первую загрузку (когда мы устанавливаем Source)
            if (_isInitialLoad)
            {
                _isInitialLoad = false;
                return;
            }

            // Отменяем все последующие переходы (по ссылкам внутри статьи)
            e.Cancel = true;
        }
        private void OnWebViewNavigated(object sender, WebNavigatedEventArgs e) { }
    }
}