using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using NewsApp.Models;
using NewsApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NewsApp.Views
{
    public partial class BookmarksPage : ContentPage
    {
        private bool _showRead = true;
        
        public BookmarksPage()
        {
            InitializeComponent();
            LoadItems();
        }

        private async void LoadItems()
        {
            try
            {
                var userId = Preferences.Get("user_id", "");
                if (App.ServiceProvider == null || string.IsNullOrEmpty(userId)) return;
                
                var db = App.ServiceProvider.GetRequiredService<LocalDatabaseService>();
                List<Article> items;
                
                if (_showRead)
                {
                    items = await db.GetReadArticlesAsync(userId);
                }
                else
                {
                    items = await db.GetFavoriteArticlesAsync(userId);
                }
                
                ItemsListView.ItemsSource = items;
            }
            catch
            {
                ItemsListView.ItemsSource = new List<Article>();
            }
        }

        private void OnReadTab(object sender, EventArgs e)
        {
            _showRead = true;
            ReadTabButton.BackgroundColor = Colors.Blue;
            ReadTabButton.TextColor = Colors.White;
            FavoriteTabButton.BackgroundColor = Colors.LightGray;
            FavoriteTabButton.TextColor = Colors.Black;
            LoadItems();
        }

        private void OnFavoriteTab(object sender, EventArgs e)
        {
            _showRead = false;
            FavoriteTabButton.BackgroundColor = Colors.Blue;
            FavoriteTabButton.TextColor = Colors.White;
            ReadTabButton.BackgroundColor = Colors.LightGray;
            ReadTabButton.TextColor = Colors.Black;
            LoadItems();
        }

        private async void OnItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is Article article)
            {
                if (!string.IsNullOrEmpty(article.Url))
                {
                    var detailPage = new ArticleDetailPage(article.Title ?? "", article.Summary ?? "", article.Source ?? "", article.Url);
                    await Navigation.PushAsync(detailPage);
                }
            }
            ((ListView)sender).SelectedItem = null;
        }
    }
}