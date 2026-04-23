using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewsApp.Models;
using NewsApp.Services;
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

        public NewsListViewModel(INewsService newsService, LocalDatabaseService db)
        {
            _newsService = newsService;
            _db = db;
            _userId = Preferences.Get("user_id", Guid.NewGuid().ToString());
            Headlines = new ObservableCollection<Article>();
        }

        public async Task<List<string>> GetSelectedCategories()
        {
            return await _db.GetUserCategoriesAsync(_userId);
        }

        [RelayCommand]
        public async Task LoadHeadlines()
        {
            IsRefreshing = true;
            var categories = await GetSelectedCategories();

            if (categories == null || categories.Count == 0)
            {
                IsRefreshing = false;
                await Shell.Current.GoToAsync("//CategorySelectionPage");
                return;
            }

            var articles = await _newsService.GetHeadlinesAsync(categories);
            Headlines.Clear();
            foreach (var art in articles)
                Headlines.Add(art);
            IsRefreshing = false;
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