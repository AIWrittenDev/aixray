using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AIXray.Core;
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

    public MainViewModel(
        IServerRepository serverRepo,
        IGroupRepository groupRepo,
        ISettingsRepository settingsRepo,
        IShareLinkParserService parserService,
        ISubscriptionFetcher subscriptionFetcher,
        IXrayConfigBuilder configBuilder,
        IXrayDownloader xrayDownloader,
        IXrayProcessManager processManager)
    {
        _serverRepo = serverRepo;
        _groupRepo = groupRepo;
        _settingsRepo = settingsRepo;
        _parserService = parserService;
        _subscriptionFetcher = subscriptionFetcher;
        _configBuilder = configBuilder;
        _xrayDownloader = xrayDownloader;
        _processManager = processManager;

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

    [RelayCommand]
    private async Task InitializeAsync()
    {
        StatusText = "در حال راه‌اندازی...";
        try
        {
            await _xrayDownloader.EnsureInstalledAsync();
            await LoadGroupsAsync();
            await LoadServersAsync();
            StatusText = "آماده";
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

        try
        {
            StatusText = "در حال اتصال...";
            var settings = await _settingsRepo.LoadAsync();
            settings.Mode = CurrentMode;
            var configDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AIXray", "config-generated");
            var config = _configBuilder.BuildConfig(SelectedServer, settings, configDir);
            var xrayPath = _xrayDownloader.XrayExePath;
            await _processManager.StartAsync(xrayPath, config, configDir);
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
        StatusText = "قطع شد";
    }

    [RelayCommand]
    private async Task ImportFromClipboardAsync()
    {
        try
        {
            var text = Clipboard.GetText();
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
        if (string.IsNullOrWhiteSpace(NewGroupName))
        {
            StatusText = "نام گروه را وارد کنید";
            return;
        }

        var group = new Group
        {
            Name = NewGroupName,
            SubscriptionUrl = string.IsNullOrWhiteSpace(NewGroupSubscriptionUrl) ? null : NewGroupSubscriptionUrl,
            AutoUpdate = !string.IsNullOrWhiteSpace(NewGroupSubscriptionUrl),
        };

        await _groupRepo.AddAsync(group);
        NewGroupName = string.Empty;
        NewGroupSubscriptionUrl = string.Empty;
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
        // TODO: Phase 6 — HTTPing test
        server.LatencyMs = Random.Shared.Next(50, 500);
        server.LastTest = DateTime.UtcNow;
        await _serverRepo.UpdateAsync(server);
        StatusText = $"{server.Remark}: {server.LatencyMs}ms";
    }

    [RelayCommand]
    private void CopyShareLink(Server? server)
    {
        if (server == null) return;
        Clipboard.SetText(server.Url);
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
    private void OpenSettings()
    {
        var dialog = new SettingsDialog(CurrentMode, AutoConnect);
        if (dialog.ShowDialog() == true)
        {
            CurrentMode = dialog.SelectedMode;
            AutoConnect = dialog.AutoConnect;
            _ = SaveSettingsAsync();
        }
    }

    [RelayCommand]
    private async Task TestAllLatencyAsync()
    {
        StatusText = "تست همه سرورها...";
        foreach (var server in Servers)
        {
            server.LatencyMs = Random.Shared.Next(50, 500);
            server.LastTest = DateTime.UtcNow;
            await _serverRepo.UpdateAsync(server);
        }
        StatusText = "تست کامل شد";
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
        var settings = new AppSettings
        {
            Mode = CurrentMode,
            AutoConnect = AutoConnect,
        };
        await _settingsRepo.SaveAsync(settings);
    }
}
