using System.Collections.ObjectModel;
using System.Windows.Input;
using NewsApp.Models;
using NewsApp.Services;
using NewsApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace NewsApp.ViewModels
{
    public class CategorySelectionViewModel
    {
        private readonly LocalDatabaseService _db;
        private string _userId;

        public ObservableCollection<Category> AvailableCategories { get; }
        public ObservableCollection<Category> SelectedCategories { get; }

        public ICommand ToggleCategoryCommand { get; }
        public ICommand SaveAndContinueCommand { get; }

        public CategorySelectionViewModel(LocalDatabaseService db)
        {
            _db = db;
            _userId = Preferences.Get("user_id", "");
            if (string.IsNullOrEmpty(_userId))
            {
                _userId = Guid.NewGuid().ToString();
                Preferences.Set("user_id", _userId);
            }

            AvailableCategories = new ObservableCollection<Category>
            {
                new Category { Id = 1, NameEn = "World", NameRu = "Мир", NytSection = "world" },
                new Category { Id = 2, NameEn = "Technology", NameRu = "Технологии", NytSection = "technology" },
                new Category { Id = 3, NameEn = "Business", NameRu = "Бизнес", NytSection = "business" },
                new Category { Id = 4, NameEn = "Sports", NameRu = "Спорт", NytSection = "sports" },
                new Category { Id = 5, NameEn = "Science", NameRu = "Наука", NytSection = "science" }
            };
            SelectedCategories = new ObservableCollection<Category>();

            ToggleCategoryCommand = new Command<Category>(ToggleCategory);
            SaveAndContinueCommand = new Command(SaveAndContinue);
        }

        private void ToggleCategory(Category category)
        {
            if (category == null) return;
            if (SelectedCategories.Contains(category))
                SelectedCategories.Remove(category);
            else
                SelectedCategories.Add(category);
        }

        private async void SaveAndContinue()
        {
            if (SelectedCategories.Count == 0)
            {
                await Shell.Current.DisplayAlert("Ошибка", "Выберите хотя бы одну категорию", "OK");
                return;
            }
            
            var selectedNames = SelectedCategories.Select(c => c.NameEn).ToList();
            await _db.SaveUserCategoriesAsync(_userId, selectedNames);
            await _db.SetOnboardingCompletedAsync(_userId, true);
            
            var db = App.ServiceProvider.GetRequiredService<LocalDatabaseService>();
            var news = App.ServiceProvider.GetRequiredService<INewsService>();
            var vm = new NewsListViewModel(news, db);
            var page = new NewsListPage(vm);
            
            Application.Current?.MainPage?.Navigation?.PushAsync(page);
        }
    }
}