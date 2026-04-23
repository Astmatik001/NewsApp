using NewsApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace NewsApp.Views
{
    public partial class CategorySelectionPage : ContentPage
    {
        public CategorySelectionPage(CategorySelectionViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }

    }
}