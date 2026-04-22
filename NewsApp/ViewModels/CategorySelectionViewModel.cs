using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewsApp.Models;
using NewsApp.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace NewsApp.ViewModels
{
    public partial class CategorySelectionViewModel : ObservableObject
    {
        private readonly LocalDatabaseService _db;
        private readonly string _userId;

        [ObservableProperty]
        private ObservableCollection<Category> availableCategories;

        [ObservableProperty]
        private ObservableCollection<Category> selectedCategories;

        public CategorySelectionViewModel(LocalDatabaseService db, string userId)
        {
            _db = db;
            _userId = userId;
            AvailableCategories = new ObservableCollection<Category>
            {
                new Category { Id = 1, NameEn = "World", NameRu = "Мир", NytSection = "world" },
                new Category { Id = 2, NameEn = "Technology", NameRu = "Технологии", NytSection = "technology" },
                new Category { Id = 3, NameEn = "Business", NameRu = "Бизнес", NytSection = "business" },
                new Category { Id = 4, NameEn = "Sports", NameRu = "Спорт", NytSection = "sports" },
                new Category { Id = 5, NameEn = "Science", NameRu = "Наука", NytSection = "science" }
            };
            SelectedCategories = new ObservableCollection<Category>();
        }

        [RelayCommand]
        private void ToggleCategory(Category category)
        {
            if (SelectedCategories.Contains(category))
                SelectedCategories.Remove(category);
            else
                SelectedCategories.Add(category);
        }

        [RelayCommand]
        private async Task SaveAndContinue()
        {
            var selectedNames = new System.Collections.Generic.List<string>();
            foreach (var cat in SelectedCategories)
                selectedNames.Add(cat.NameEn);
            await _db.SaveUserCategoriesAsync(_userId, selectedNames);
            await _db.SetOnboardingCompletedAsync(_userId, true);
            await Shell.Current.GoToAsync("//NewsListPage");
        }
    }
}