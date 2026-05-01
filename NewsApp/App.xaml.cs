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
    public static IConfiguration? Configuration { get; private set; }

    public App()
    {
        InitializeComponent();
        MainPage = new ContentPage { Content = new Label { Text = "Loading..." } };
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        Routing.RegisterRoute("//NewsListPage", typeof(NewsListPage));
        Routing.RegisterRoute("//CategorySelectionPage", typeof(CategorySelectionPage));
        Routing.RegisterRoute("//ArticleDetailPage", typeof(ArticleDetailPage));
        Routing.RegisterRoute("//BookmarksPage", typeof(BookmarksPage));
        Routing.RegisterRoute("//PremiumPage", typeof(PremiumPage));
    }

    protected override async void OnStart()
    {
        base.OnStart();

        try
        {
            Configuration = LoadConfiguration();
            
            var services = new ServiceCollection();
            ConfigureServices(services, Configuration);
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
                        new Label { Text = ex.StackTrace ?? "", LineBreakMode = LineBreakMode.WordWrap, FontSize = 10 }
                    }
                }
            };
        }
    }

    private IConfiguration LoadConfiguration()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            
            // For .NET MAUI, embedded resource name is: Namespace.filename
            var resourceName = resourceNames.FirstOrDefault(n => 
                n.EndsWith("appsettings.json", StringComparison.OrdinalIgnoreCase));
            
            if (resourceName == null)
            {
                throw new Exception($"appsettings.json not found. Available resources: {string.Join(", ", resourceNames)}");
            }
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new Exception($"Failed to load embedded resource: {resourceName}");
                
            return new ConfigurationBuilder().AddJsonStream(stream).Build();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load configuration: {ex.Message}");
        }
    }

    private void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        var cambAiApiKey = config["ApiSettings:CambAiApiKey"] ?? "";
        var openRouterApiKey = config["ApiSettings:OpenRouterApiKey"] ?? "";
        var openRouterEndpoint = config["ApiSettings:OpenRouterEndpoint"] ?? "https://openrouter.ai/api/v1/chat/completions";
        var grammarModel = config["ApiSettings:GrammarAnalysisModel"] ?? "deepseek/deepseek-chat-v3:free";

        services.AddSingleton(new LocalDatabaseService());
        services.AddSingleton(new TranslationService(""));
        services.AddSingleton(new GrammarAnalysisService(openRouterApiKey, openRouterEndpoint, grammarModel));
        services.AddSingleton<INewsService, RssService>();
        services.AddSingleton<IAudioManager>(AudioManager.Current);
        services.AddSingleton(new CambAiTtsService(cambAiApiKey));
        services.AddSingleton(provider => new AnalyticsService("", Preferences.Get("user_id", Guid.NewGuid().ToString())));

        services.AddTransient<CategorySelectionViewModel>();
        services.AddTransient<NewsListViewModel>();
        services.AddTransient<ArticleDetailViewModel>();
    }
}
