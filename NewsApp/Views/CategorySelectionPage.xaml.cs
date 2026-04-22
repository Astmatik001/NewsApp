using NewsApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace NewsApp.Views
{
    public partial class CategorySelectionPage : ContentPage
    {
        public CategorySelectionPage()
        {
            InitializeComponent();
            var vm = App.ServiceProvider.GetRequiredService<CategorySelectionViewModel>();
            BindingContext = vm;
        }

    }
}