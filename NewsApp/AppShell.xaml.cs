using NewsApp.Views;
using Microsoft.Extensions.DependencyInjection;

namespace NewsApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("ArticleDetailPage", typeof(ArticleDetailPage));
        }
    }
}