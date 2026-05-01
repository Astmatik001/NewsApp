using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewsApp.Models;
using NewsApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NewsApp.ViewModels
{
    public partial class NewsListViewModel : ObservableObject
    {
        private readonly INewsService _newsService;
        private readonly LocalDatabaseService _db;
        private readonly string _userId;

        [ObservableProperty]
        private ObservableCollection<Article> headlines;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private bool isListVisible;

        [ObservableProperty]
        private bool isEmptyVisible;

        [ObservableProperty]
        private string? errorMessage;

        [ObservableProperty]
        private bool showLimitMessage;

        public NewsListViewModel(INewsService newsService, LocalDatabaseService db)
        {
            _newsService = newsService;
            _db = db;
            _userId = Preferences.Get("user_id", "");
            if (string.IsNullOrEmpty(_userId))
            {
                _userId = Guid.NewGuid().ToString();
                Preferences.Set("user_id", _userId);
            }
            Headlines = new ObservableCollection<Article>();
            System.Diagnostics.Debug.WriteLine("NewsListViewModel created, loading headlines...");
            LoadHeadlines();
        }

        public async Task<List<string>> GetSelectedCategories()
        {
            return await _db.GetUserCategoriesAsync(_userId);
        }

        public async Task LoadHeadlinesAsync()
        {
            await LoadHeadlines();
        }

        public async Task LoadHeadlines()
        {
            try
            {
                IsRefreshing = true;
                ErrorMessage = string.Empty;
                IsListVisible = false;
                IsEmptyVisible = false;
                ShowLimitMessage = false;

                var categories = await GetSelectedCategories();
                System.Diagnostics.Debug.WriteLine($"Categories: {categories?.Count ?? 0}");

                // TEMP: If no categories selected, use Technology by default for testing
                if (categories == null || categories.Count == 0)
                {
                    categories = new List<string> { "Technology" };
                    System.Diagnostics.Debug.WriteLine("No categories selected, using Technology as default for testing");
                }

                var articles = await _newsService.GetHeadlinesAsync(categories);
                System.Diagnostics.Debug.WriteLine($"Articles: {articles.Count} for categories: {string.Join(", ", categories)}");
                
                // Apply plan limits
                var userPlan = Preferences.Get("user_plan", "free");
                
                Headlines.Clear();
                
                if (userPlan == "free")
                {
                    // Check daily read limit (3 per day)
                    var dailyReads = await _db.GetDailyReadCountAsync(_userId);
                    if (dailyReads >= 3)
                    {
                        // Already reached daily limit
                        IsRefreshing = false;
                        IsEmptyVisible = true;
                        ShowLimitMessage = true;
                        ErrorMessage = "Вы достигли лимита бесплатного тарифа (3 новости в день).";
                        return;
                    }
                    
                    // Show max 3 articles TOTAL for free users
                    int remaining = 3 - dailyReads;
                    foreach (var art in articles.Take(remaining))
                        Headlines.Add(art);
                }
                else
                {
                    // Premium/Pro users get unlimited articles
                    foreach (var art in articles)
                        Headlines.Add(art);
                }
                
                IsListVisible = Headlines.Count > 0;
                IsEmptyVisible = Headlines.Count == 0;
                
                if (IsEmptyVisible && !ShowLimitMessage)
                    ErrorMessage = "Нет новостей. Проверьте интернет или выберите другие категории.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                ErrorMessage = $"Ошибка загрузки: {ex.Message}";
                IsEmptyVisible = true;
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task OpenArticle(Article article)
        {
            if (article == null || string.IsNullOrEmpty(article.Url))
            {
                await Shell.Current.DisplayAlert("Error", "Article URL is missing", "OK");
                return;
            }
            try
            {
                await Shell.Current.GoToAsync($"ArticleDetailPage?url={Uri.EscapeDataString(article.Url)}");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Navigation Error", ex.Message, "OK");
            }
        }
    }
}