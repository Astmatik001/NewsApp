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

        [RelayCommand]
        private async Task LoadHeadlines()
        {
            IsRefreshing = true;
            var categories = await _db.GetUserCategoriesAsync(_userId);
            var articles = await _newsService.GetHeadlinesAsync(categories);
            Headlines.Clear();
            foreach (var art in articles)
                Headlines.Add(art);
            IsRefreshing = false;
        }

        [RelayCommand]
        private async Task OpenArticle(Article article)
        {
            var parameters = new ShellNavigationQueryParameters
            {
                { "url", article.Url }
            };
            await Shell.Current.GoToAsync($"ArticleDetailPage?url={Uri.EscapeDataString(article.Url)}");
        }
    }
}