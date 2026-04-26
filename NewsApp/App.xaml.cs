using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsApp.Services;
using NewsApp.ViewModels;
using NewsApp.Views;
using Plugin.Maui.Audio;

namespace NewsApp;

public partial class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    public App()
    {
        InitializeComponent();
        // Temporary loading screen
        MainPage = new ContentPage { Content = new Label { Text = "Loading..." } };
    }

    protected override async void OnStart()
    {
        base.OnStart();

        try
        {
            var config = await LoadConfigurationAsync();
            var services = new ServiceCollection();
            ConfigureServices(services, config);
            ServiceProvider = services.BuildServiceProvider();

            var db = ServiceProvider.GetRequiredService<LocalDatabaseService>();
            var userId = Preferences.Get("user_id", Guid.NewGuid().ToString());
            Preferences.Set("user_id", userId);
            var onboardingCompleted = await db.GetOnboardingCompletedAsync(userId);

            Page firstPage;
            if (onboardingCompleted)
            {
                var vm = ServiceProvider.GetRequiredService<NewsListViewModel>();
                firstPage = new NewsListPage(vm);
            }
            else
            {
                var vm = ServiceProvider.GetRequiredService<CategorySelectionViewModel>();
                firstPage = new CategorySelectionPage(vm);
            }

            MainPage = new NavigationPage(firstPage);
        }
        catch (Exception ex)
        {
            MainPage = new ContentPage
            {
                Content = new StackLayout
                {
                    Padding = 20,
                    Spacing = 10,
                    Children =
                    {
                        new Label { Text = "Startup Error", FontAttributes = FontAttributes.Bold, FontSize = 20 },
                        new Label { Text = ex.Message, LineBreakMode = LineBreakMode.WordWrap },
                        new Label { Text = ex.StackTrace, LineBreakMode = LineBreakMode.WordWrap, FontSize = 10 }
                    }
                }
            };
        }
    }

    private async Task<IConfiguration> LoadConfigurationAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("NewsApp.appsettings.json");
        if (stream == null)
            throw new Exception("appsettings.json not found as embedded resource. Build Action must be 'Embedded resource'.");
        return new ConfigurationBuilder().AddJsonStream(stream).Build();
    }

    private void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        var deepSeekKey = config["ApiSettings:DeepSeekApiKey"];
        var cambAiKey = config["ApiSettings:CambAiApiKey"];
        var analyticsUrl = config["ApiSettings:AnalyticsBackendUrl"];

        services.AddSingleton(new LocalDatabaseService());
        services.AddSingleton(new DeepSeekService(deepSeekKey ?? "dummy"));
        services.AddSingleton(new CambAiTtsService(cambAiKey ?? "dummy"));
        services.AddSingleton<INewsService, RssService>();
        services.AddSingleton<IAudioManager>(AudioManager.Current);
        services.AddSingleton(provider => new AnalyticsService(analyticsUrl ?? "", Preferences.Get("user_id", Guid.NewGuid().ToString())));
        services.AddTransient<CategorySelectionViewModel>();
        services.AddTransient<NewsListViewModel>();
        services.AddTransient<ArticleDetailViewModel>();
    }
}