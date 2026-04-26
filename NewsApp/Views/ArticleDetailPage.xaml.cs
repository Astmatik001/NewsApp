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

        public ArticleDetailPage()
        {
            InitializeComponent();
            OpenUrlButton.Clicked += OnOpenUrlClicked;
            
            ReadCheckBox.CheckedChanged += async (s, e) => {
                ReadLabel.Text = ReadCheckBox.IsChecked ? "Прочитано ✓" : "Прочитано";
                ReadLabel.TextColor = ReadCheckBox.IsChecked ? Colors.Green : Colors.Gray;
                if (ReadCheckBox.IsChecked && !string.IsNullOrEmpty(ArticleUrl))
                {
                    await SaveReadStatus();
                }
            };
            
            FavoriteCheckBox.CheckedChanged += async (s, e) => {
                FavoriteLabel.Text = FavoriteCheckBox.IsChecked ? "В избранном ★" : "В избранное";
                FavoriteLabel.TextColor = FavoriteCheckBox.IsChecked ? Colors.Orange : Colors.Gray;
                if (FavoriteCheckBox.IsChecked && !string.IsNullOrEmpty(ArticleUrl))
                {
                    await SaveFavoriteStatus();
                }
            };
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

        public ArticleDetailPage(string title, string summary, string source, string url) : this()
        {
            Title = title;
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
            
            ContentLabel.Text = "Загрузка...";
            string summaryCopy = SummaryLabel.Text ?? "";
            
            try
            {
                var content = await FetchArticleContentAsync(url);
                if (!string.IsNullOrEmpty(content))
                {
                    ContentLabel.Text = content;
                }
                else
                {
                    ContentLabel.Text = summaryCopy + "\n\n" + "Полный текст недоступен. Нажмите кнопку ниже для открытия.";
                }
            }
            catch (Exception ex)
            {
                ContentLabel.Text = $"Ошибка загрузки: {ex.Message}";
            }
        }

        private async Task<string> FetchArticleContentAsync(string url)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var html = await client.GetStringAsync(url);
                
                var startTag = "<p";
                var endTag = "</p>";
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
                            {
                                content.AppendLine(text.Trim());
                            }
                        }
                    }
                    index = closeIndex + 1;
                }
                
                var result = content.ToString().Trim();
                if (string.IsNullOrEmpty(result))
                {
                    return null;
                }
                return result;
            }
            catch
            {
                return null;
            }
        }

        private async void OnOpenUrlClicked(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(ArticleUrl))
            {
                await Launcher.OpenAsync(ArticleUrl);
            }
        }
    }
}