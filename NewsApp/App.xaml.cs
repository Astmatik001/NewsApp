using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsApp.Services;
using NewsApp.ViewModels;
using NewsApp.Views;
using Plugin.Maui.Audio;
using SCollection = Microsoft.Extensions.DependencyInjection.ServiceCollection;

namespace NewsApp;

public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        Routing.RegisterRoute("//NewsListPage", typeof(NewsListPage));
        Routing.RegisterRoute("//CategorySelectionPage", typeof(CategorySelectionPage));
        Routing.RegisterRoute("//ArticleDetailPage", typeof(ArticleDetailPage));
        Routing.RegisterRoute("//BookmarksPage", typeof(BookmarksPage));
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        try 
        {
            var services = new SCollection();
            services.AddSingleton(new LocalDatabaseService());
            services.AddSingleton<INewsService, RssService>();
            services.AddTransient<NewsListViewModel>();
            services.AddTransient<CategorySelectionViewModel>();
            services.AddTransient<ArticleDetailViewModel>();
            
            ServiceProvider = services.BuildServiceProvider();
            
            var vm = ServiceProvider.GetRequiredService<CategorySelectionViewModel>();
            var page = new CategorySelectionPage(vm);
            
            return new Window(new NavigationPage(page));
        }
        catch (Exception ex)
        {
            return new Window(new ContentPage { Content = new Label { Text = $"Error: {ex.Message}" } });
        }
    }
}