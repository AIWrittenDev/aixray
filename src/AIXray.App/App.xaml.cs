using System.IO;
using System.Windows;
using AIXray.Core;
using AIXray.Network;
using AIXray.Proxies;
using AIXray.Storage;
using AIXray.ShareLinks;
using AIXray.Xray;
using Microsoft.Extensions.DependencyInjection;

namespace AIXray.App;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AIXray", "data");
        var dbPath = Path.Combine(dataDir, "aixray.db");

        services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory(dbPath));
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton<IServerRepository, ServerRepository>();
        services.AddSingleton<IGroupRepository, GroupRepository>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();

        services.AddSingleton<IShareLinkParser, VlessParser>();
        services.AddSingleton<IShareLinkParser, VmessParser>();
        services.AddSingleton<IShareLinkParser, TrojanParser>();
        services.AddSingleton<IShareLinkParser, ShadowsocksParser>();
        services.AddSingleton<IShareLinkParserService, ShareLinkParserService>();
        services.AddSingleton<ISubscriptionFetcher, SubscriptionFetcher>();

        services.AddSingleton<IXrayConfigBuilder, XrayConfigBuilder>();
        services.AddSingleton<IXrayDownloader, XrayDownloader>();
        services.AddSingleton<IXrayProcessManager, XrayProcessManager>();

        services.AddSingleton<IServerTester, ServerTester>();
        services.AddSingleton<IAutoConnectService, AutoConnectService>();
        services.AddSingleton<ISystemProxyManager, SystemProxyManager>();
        services.AddSingleton<ITunManager, TunManager>();

        services.AddSingleton<MainViewModel>();
    }
}
