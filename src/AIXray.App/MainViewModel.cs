using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIXray.Core;
using AIXray.Network;
using AIXray.Proxies;
using AIXray.Storage;
using AIXray.ShareLinks;
using AIXray.Xray;

namespace AIXray.App;

public partial class MainViewModel : ObservableObject
{
    private readonly IServerRepository _serverRepo;
    private readonly IGroupRepository _groupRepo;
    private readonly ISettingsRepository _settingsRepo;
    private readonly IShareLinkParserService _parserService;
    private readonly ISubscriptionFetcher _subscriptionFetcher;
    private readonly IXrayConfigBuilder _configBuilder;
    private readonly IXrayDownloader _xrayDownloader;
    private readonly IXrayProcessManager _processManager;
    private readonly IServerTester _serverTester;
    private readonly IAutoConnectService _autoConnectService;
    private readonly ISystemProxyManager _systemProxyManager;
    private readonly ITunManager _tunManager;

    public MainViewModel(
        IServerRepository serverRepo,
        IGroupRepository groupRepo,
        ISettingsRepository settingsRepo,
        IShareLinkParserService parserService,
        ISubscriptionFetcher subscriptionFetcher,
        IXrayConfigBuilder configBuilder,
        IXrayDownloader xrayDownloader,
        IXrayProcessManager processManager,
        IServerTester serverTester,
        IAutoConnectService autoConnectService,
        ISystemProxyManager systemProxyManager,
        ITunManager tunManager)
    {
        _serverRepo = serverRepo;
        _groupRepo = groupRepo;
        _settingsRepo = settingsRepo;
        _parserService = parserService;
        _subscriptionFetcher = subscriptionFetcher;
        _configBuilder = configBuilder;
        _xrayDownloader = xrayDownloader;
        _processManager = processManager;
        _serverTester = serverTester;
        _autoConnectService = autoConnectService;
        _systemProxyManager = systemProxyManager;
        _tunManager = tunManager;

        _processManager.LogReceived += (_, log) => LogEntries.Add(log);
        _processManager.StateChanged += (_, running) =>
        {
            IsConnected = running;
            OnPropertyChanged(nameof(ConnectionStatusText));
        };
    }

    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _statusText = "آماده";
    [ObservableProperty] private Server? _selectedServer;
    [ObservableProperty] private Group? _selectedGroup;
    [ObservableProperty] private ConnectionMode _currentMode = ConnectionMode.SystemProxy;
    [ObservableProperty] private bool _autoConnect;
    [ObservableProperty] private string _newGroupSubscriptionUrl = string.Empty;
    [ObservableProperty] private string _newGroupName = string.Empty;

    public ObservableCollection<Server> Servers { get; } = new();
    public ObservableCollection<Group> Groups { get; } = new();
    public ObservableCollection<string> LogEntries { get; } = new();

    public string ConnectionStatusText => IsConnected ? "متصل" : "قطع";

    // وقتی حالت تغییر کند و متصل باشیم، اتصال مجددا برقرار می‌شود
    partial void OnCurrentModeChanged(ConnectionMode value)
    {
        if (IsConnected && SelectedServer != null)
        {
            _ = RestartWithNewModeAsync();
        }
    }

    private async Task RestartWithNewModeAsync()
    {
        StatusText = "تغییر حالت - اتصال مجدد...";
        await DisconnectAsync();
        await Task.Delay(300);
        await ConnectAsync();
    }

    [RelayCommand]
    private async Task ToggleConnectionAsync()
    {
        if (IsConnected)
            await DisconnectAsync();
        else
            await ConnectAsync();
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        StatusText = "در حال راه‌اندازی...";
        try
        {
            await _xrayDownloader.EnsureInstalledAsync();
            await LoadGroupsAsync();
            await LoadServersAsync();

            if (AutoConnect)
            {
                StatusText = "اتصال خودکار...";
                var settings = await _settingsRepo.LoadAsync();
                settings.Mode = CurrentMode;
                var connected = await _autoConnectService.ConnectToBestAsync(settings);
                if (connected)
                    StatusText = "اتصال خودکار: متصل";
                else
                    StatusText = "اتصال خودکار: سرور فعالی یافت نشد";
            }
            else
            {
                StatusText = "آماده";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"خطا: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadGroupsAsync()
    {
        Groups.Clear();
        var groups = await _groupRepo.GetAllAsync();
        foreach (var g in groups)
            Groups.Add(g);
    }

    [RelayCommand]
    private async Task LoadServersAsync()
    {
        Servers.Clear();
        var servers = await _serverRepo.GetAllAsync();
        foreach (var s in servers)
            Servers.Add(s);
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (SelectedServer == null)
        {
            StatusText = "سروری انتخاب نشده است";
            return;
        }

        // بررسی حالت TUN و دسترسی admin
        if (CurrentMode == ConnectionMode.Tun && !_tunManager.IsRunningAsAdmin)
        {
            StatusText = "حالت TUN نیاز به دسترسی admin دارد - در حال اجرای مجدد...";
            await Task.Delay(500);
            if (_tunManager.RestartAsAdmin())
            {
                System.Windows.Application.Current.Shutdown();
            }
            return;
        }

        try
        {
            StatusText = "در حال اتصال...";

            // غیرفعال‌سازی همه و فعال‌سازی سرور انتخاب شده
            await _serverRepo.DeactivateAllAsync();
            await _serverRepo.SetActiveAsync(SelectedServer.Id, active: true);

            var settings = await _settingsRepo.LoadAsync();
            settings.Mode = CurrentMode;
            var configDir = Path.Combine(
                AppContext.BaseDirectory, "config-generated");
            var config = _configBuilder.BuildConfig(SelectedServer, settings, configDir);
            await _processManager.StartAsync(_xrayDownloader.XrayExePath, config, configDir);

            // اعمال پروکسی سیستم
            if (CurrentMode == ConnectionMode.SystemProxy)
            {
                _systemProxyManager.Enable(settings.LocalPort);
            }

            StatusText = "متصل";
        }
        catch (Exception ex)
        {
            StatusText = $"خطا: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _processManager.StopAsync();
        _systemProxyManager.Disable();
        StatusText = "قطع شد";
    }

    [RelayCommand]
    private async Task ImportFromClipboardAsync()
    {
        try
        {
            var text = System.Windows.Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text)) return;

            var servers = _parserService.ParseLinks(text);
            if (servers.Count == 0)
            {
                StatusText = "لینک معتبری یافت نشد";
                return;
            }

            foreach (var server in servers)
                await _serverRepo.AddAsync(server);

            await LoadServersAsync();
            StatusText = $"{servers.Count} سرور وارد شد";
        }
        catch (Exception ex)
        {
            StatusText = $"خطا: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ImportFromFileAsync()
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "فایل‌های متنی|*.txt;*.dat|همه فایل‌ها|*.*",
                Title = "انتخاب فایل کانفیگ",
            };

            if (dialog.ShowDialog() != true) return;

            var content = await File.ReadAllTextAsync(dialog.FileName);
            var servers = _parserService.ParseLinks(content);
            if (servers.Count == 0)
            {
                StatusText = "لینک معتبری یافت نشد";
                return;
            }

            foreach (var server in servers)
                await _serverRepo.AddAsync(server);

            await LoadServersAsync();
            StatusText = $"{servers.Count} سرور وارد شد";
        }
        catch (Exception ex)
        {
            StatusText = $"خطا: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CreateGroupAsync()
    {
        var dialog = new CreateGroupDialog();
        if (dialog.ShowDialog() != true) return;

        var group = new Group
        {
            Name = dialog.GroupName,
            SubscriptionUrl = dialog.SubscriptionUrl,
            AutoUpdate = dialog.AutoUpdate,
            UpdateIntervalMinutes = dialog.UpdateIntervalMinutes,
        };

        await _groupRepo.AddAsync(group);
        await LoadGroupsAsync();
        StatusText = $"گروه '{group.Name}' ایجاد شد";
    }

    [RelayCommand]
    private async Task UpdateSubscriptionAsync(Group? group)
    {
        if (group == null || string.IsNullOrWhiteSpace(group.SubscriptionUrl))
        {
            StatusText = "سابسکریپشن ندارد";
            return;
        }

        try
        {
            StatusText = "در حال بروزرسانی...";
            var content = await _subscriptionFetcher.FetchAndDecodeAsync(group.SubscriptionUrl);
            var servers = _parserService.ParseLinks(content);

            foreach (var server in servers)
            {
                server.GroupId = group.Id;
                await _serverRepo.AddAsync(server);
            }

            group.LastUpdate = DateTime.UtcNow;
            await _groupRepo.UpdateAsync(group);
            await LoadServersAsync();
            StatusText = $"{servers.Count} سرور بروزرسانی شد";
        }
        catch (Exception ex)
        {
            StatusText = $"خطا: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteServerAsync(Server? server)
    {
        if (server == null) return;
        await _serverRepo.DeleteAsync(server.Id);
        await LoadServersAsync();
        StatusText = "سرور حذف شد";
    }

    [RelayCommand]
    private async Task TestLatencyAsync(Server? server)
    {
        if (server == null) return;
        StatusText = $"تست {server.Remark}...";
        try
        {
            var latency = await _serverTester.TestLatencyAsync(server);
            server.LatencyMs = latency;
            server.LastTest = DateTime.UtcNow;
            await _serverRepo.UpdateAsync(server);
            StatusText = latency.HasValue
                ? $"{server.Remark}: {latency}ms"
                : $"{server.Remark}: ناموفق";
        }
        catch
        {
            StatusText = $"تست {server.Remark}: خطا";
        }
    }

    [RelayCommand]
    private async Task TestAllLatencyAsync()
    {
        StatusText = "تست همه سرورها...";
        foreach (var server in Servers)
        {
            try
            {
                var latency = await _serverTester.TestLatencyAsync(server);
                server.LatencyMs = latency;
                server.LastTest = DateTime.UtcNow;
                await _serverRepo.UpdateAsync(server);
            }
            catch { }
        }
        StatusText = "تست کامل شد";
    }

    [RelayCommand]
    private void EditServer(Server? server)
    {
        if (server == null) return;
        var dialog = new EditServerDialog(server);
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            _ = UpdateServerAsync(dialog.Result);
        }
    }

    [RelayCommand]
    private async Task UpdateServerAsync(Server server)
    {
        await _serverRepo.UpdateAsync(server);
        await LoadServersAsync();
        StatusText = $"سرور '{server.Remark}' بروزرسانی شد";
    }

    [RelayCommand]
    private void CopyShareLink(Server? server)
    {
        if (server == null) return;
        System.Windows.Clipboard.SetText(server.Url);
        StatusText = "لینک کپی شد";
    }

    [RelayCommand]
    private void OpenCustomConfig()
    {
        var dialog = new CustomConfigDialog();
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            _ = AddCustomServerAsync(dialog.Result);
        }
    }

    [RelayCommand]
    private async Task AddCustomServerAsync(Server server)
    {
        await _serverRepo.AddAsync(server);
        await LoadServersAsync();
        StatusText = $"سرور '{server.Remark}' اضافه شد";
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var currentSettings = await _settingsRepo.LoadAsync();
        var dialog = new SettingsDialog(currentSettings);
        if (dialog.ShowDialog() == true)
        {
            currentSettings.Mode = dialog.SelectedMode;
            currentSettings.AutoConnect = dialog.AutoConnect;
            currentSettings.LogLevel = dialog.SelectedLogLevel;
            currentSettings.LocalPort = dialog.LocalPort;
            currentSettings.Language = dialog.SelectedLanguage;
            CurrentMode = dialog.SelectedMode;
            AutoConnect = dialog.AutoConnect;
            await _settingsRepo.SaveAsync(currentSettings);
            StatusText = "تنظیمات ذخیره شد";
        }
    }

    [RelayCommand]
    private async Task UpdateAllSubscriptionsAsync()
    {
        foreach (var group in Groups)
        {
            if (!string.IsNullOrWhiteSpace(group.SubscriptionUrl))
                await UpdateSubscriptionAsync(group);
        }
    }

    public async Task SaveSettingsAsync()
    {
        var settings = await _settingsRepo.LoadAsync();
        settings.Mode = CurrentMode;
        settings.AutoConnect = AutoConnect;
        await _settingsRepo.SaveAsync(settings);
    }
}
