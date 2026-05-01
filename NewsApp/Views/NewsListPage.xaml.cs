using NewsApp.ViewModels;
using NewsApp.Services;
using NewsApp.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace NewsApp.Views
{
    public partial class NewsListPage : ContentPage
    {
        public NewsListPage()
        {
            InitializeComponent();
            if (App.ServiceProvider == null) return;
            var db = App.ServiceProvider.GetRequiredService<LocalDatabaseService>();
            var news = App.ServiceProvider.GetRequiredService<INewsService>();
            var vm = new NewsListViewModel(news, db);
            BindingContext = vm;
            LoadCategories();
        }
        public NewsListPage(NewsListViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            LoadCategories();
            if (vm.Headlines.Count == 0)
            {
                vm.LoadHeadlines();
            }
        }

        private async void LoadCategories()
        {
            try
            {
                var userId = Preferences.Get("user_id", "");
                if (App.ServiceProvider != null && !string.IsNullOrEmpty(userId))
                {
                    var db = App.ServiceProvider.GetRequiredService<LocalDatabaseService>();
                    var cats = await db.GetUserCategoriesAsync(userId);
                    if (cats != null && cats.Count > 0)
                    {
                        CategoriesLabel.Text = string.Join(", ", cats);
                        System.Diagnostics.Debug.WriteLine($"Loaded categories: {string.Join(", ", cats)}");
                    }
                    else
                    {
                        CategoriesLabel.Text = "Не выбраны";
                        System.Diagnostics.Debug.WriteLine("No categories selected");
                    }
                }
            }
            catch (Exception ex)
            {
                CategoriesLabel.Text = "Не выбраны";
                System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
            }
        }

        private async void OnChangeCategories(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CategorySelectionPage(
                App.ServiceProvider.GetRequiredService<CategorySelectionViewModel>()));
        }

        private async void OnBookmarksClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new BookmarksPage());
        }

        private async void OnPremiumClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PremiumPage());
        }

        private async void OnChangeSubscription(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PremiumPage());
        }
        
        private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Article article)
            {
                if (!string.IsNullOrEmpty(article.Url))
                {
                    var detailPage = new ArticleDetailPage(
                        article.Title ?? "", 
                        article.Summary ?? "", 
                        article.Source ?? "",
                        article.Url);
                    await Navigation.PushAsync(detailPage);
                }
            }
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}