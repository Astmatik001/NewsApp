using CommunityToolkit.Mvvm.ComponentModel;
using NewsApp.Models;
using NewsApp.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace NewsApp.ViewModels
{
    public class CategorySelectionViewModel : ObservableObject
    {
        private readonly LocalDatabaseService _db;
        private readonly string _userId;

        public ObservableCollection<Category> AvailableCategories { get; }
        public ObservableCollection<Category> SelectedCategories { get; }

        public ICommand ToggleCategoryCommand { get; }
        public ICommand SaveAndContinueCommand { get; }

        public CategorySelectionViewModel(LocalDatabaseService db)
        {
            _db = db;
            _userId = Preferences.Get("user_id", Guid.NewGuid().ToString());

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
            var selectedNames = SelectedCategories.Select(c => c.NameEn).ToList();
            await _db.SaveUserCategoriesAsync(_userId, selectedNames);
            await _db.SetOnboardingCompletedAsync(_userId, true);
            await Shell.Current.GoToAsync("//NewsListPage");
        }
    }
}