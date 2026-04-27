using NewsApp.ViewModels;
using NewsApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace NewsApp.Views
{
    public partial class CategorySelectionPage : ContentPage
    {
        private int _count = 0;
        
        public CategorySelectionPage(CategorySelectionViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
            
            Cat1.CheckedChanged += OnCheckChanged;
            Cat2.CheckedChanged += OnCheckChanged;
            Cat3.CheckedChanged += OnCheckChanged;
            Cat4.CheckedChanged += OnCheckChanged;
            Cat5.CheckedChanged += OnCheckChanged;
        }
        
        private void OnCheckChanged(object sender, CheckedChangedEventArgs e)
        {
            _count = 0;
            if (Cat1.IsChecked) _count++;
            if (Cat2.IsChecked) _count++;
            if (Cat3.IsChecked) _count++;
            if (Cat4.IsChecked) _count++;
            if (Cat5.IsChecked) _count++;
            CountLabel.Text = $"Выбрано: {_count}";
        }

        private void OnCat1Tapped(object sender, EventArgs e) => Cat1.IsChecked = !Cat1.IsChecked;
        private void OnCat2Tapped(object sender, EventArgs e) => Cat2.IsChecked = !Cat2.IsChecked;
        private void OnCat3Tapped(object sender, EventArgs e) => Cat3.IsChecked = !Cat3.IsChecked;
        private void OnCat4Tapped(object sender, EventArgs e) => Cat4.IsChecked = !Cat4.IsChecked;
        private void OnCat5Tapped(object sender, EventArgs e) => Cat5.IsChecked = !Cat5.IsChecked;

        private async void OnContinue(object sender, EventArgs e)
        {
            if (_count == 0)
            {
                await DisplayAlert("Ошибка", "Выберите хотя бы одну тему", "OK");
                return;
            }
            
            var selected = new System.Collections.Generic.List<string>();
            if (Cat1.IsChecked) selected.Add("World");
            if (Cat2.IsChecked) selected.Add("Technology");
            if (Cat3.IsChecked) selected.Add("Business");
            if (Cat4.IsChecked) selected.Add("Sports");
            if (Cat5.IsChecked) selected.Add("Science");
            
            var userId = Preferences.Get("user_id", "");
            if (string.IsNullOrEmpty(userId))
            {
                userId = Guid.NewGuid().ToString();
                Preferences.Set("user_id", userId);
            }
            
            if (App.ServiceProvider != null)
            {
                var db = App.ServiceProvider.GetRequiredService<LocalDatabaseService>();
                await db.SaveUserCategoriesAsync(userId, selected);
                await db.SetOnboardingCompletedAsync(userId, true);
                
                var news = App.ServiceProvider.GetRequiredService<NewsApp.Services.INewsService>();
                var vm = new NewsListViewModel(news, db);
                var page = new NewsListPage(vm);
                await Navigation.PushAsync(page);
            }
        }
    }
}