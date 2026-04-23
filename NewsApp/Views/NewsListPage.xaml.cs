using NewsApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace NewsApp.Views
{
    public partial class NewsListPage : ContentPage
    {
        public NewsListPage()
        {
            InitializeComponent();
            var vm = App.ServiceProvider.GetRequiredService<NewsListViewModel>();
            BindingContext = vm;
        }
        public NewsListPage(NewsListViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var vm = BindingContext as NewsListViewModel;
            if (vm != null)
            {
                var categories = await vm.GetSelectedCategories();
                if (categories == null || categories.Count == 0)
                {
                    await DisplayAlert("Нет категорий", "Пожалуйста, выберите интересы", "OK");
                    await Shell.Current.GoToAsync("//CategorySelectionPage");
                }
                else
                {
                    if (vm.Headlines.Count == 0)
                        await vm.LoadHeadlines();
                }
            }
        }
    }
}