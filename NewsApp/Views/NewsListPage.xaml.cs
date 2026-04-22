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
    }
}