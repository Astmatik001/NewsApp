using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsApp.Services;
using NewsApp.ViewModels;
using NewsApp.Views;
using Plugin.Maui.Audio;

namespace NewsApp;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; }

    public App()
    {
        InitializeComponent();
        MainPage = new ContentPage { Content = new Label { Text = "Loading..." } };
    }

    protected override void OnStart()
    {
        base.OnStart();

        var config = Current?.Handler?.MauiContext?.Services.GetService<IConfiguration>();
        if (config == null)
        {
            MainPage = new ContentPage { Content = new Label { Text = "Configuration error" } };
            return;
        }

        var services = new ServiceCollection();
        ConfigureServices(services, config);
        ServiceProvider = services.BuildServiceProvider();

        var db = ServiceProvider.GetRequiredService<LocalDatabaseService>();
        var userId = Preferences.Get("user_id", Guid.NewGuid().ToString());
        Preferences.Set("user_id", userId);
        var onboardingCompleted = db.GetOnboardingCompletedAsync(userId).GetAwaiter().GetResult();

        try
        {
            Page firstPage = onboardingCompleted
                ? new NewsListPage(ServiceProvider.GetRequiredService<NewsListViewModel>())
                : new CategorySelectionPage(ServiceProvider.GetRequiredService<CategorySelectionViewModel>());
            MainPage = new NavigationPage(firstPage);
        }
        catch (Exception ex)
        {
            MainPage = new ContentPage { Content = new Label { Text = $"Error: {ex.Message}" } };
        }
    }

    private void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        var deepSeekKey = config["ApiSettings:DeepSeekApiKey"];
        var cambAiKey = config["ApiSettings:CambAiApiKey"];
        var analyticsUrl = config["ApiSettings:AnalyticsBackendUrl"];

        services.AddSingleton(new LocalDatabaseService());
        services.AddSingleton(new DeepSeekService(deepSeekKey));
        services.AddSingleton(new CambAiTtsService(cambAiKey));
        services.AddSingleton<INewsService, RssService>();
        services.AddSingleton<IAudioManager>(AudioManager.Current);

        services.AddSingleton(provider =>
        {
            var userId = Preferences.Get("user_id", Guid.NewGuid().ToString());
            return new AnalyticsService(analyticsUrl, userId);
        });

        services.AddTransient<CategorySelectionViewModel>();
        services.AddTransient<NewsListViewModel>();
        services.AddTransient<ArticleDetailViewModel>();
    }
}