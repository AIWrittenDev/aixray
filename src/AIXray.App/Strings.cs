using System.Globalization;
using System.Resources;

namespace AIXray.App;

/// <summary>
/// دسترسی به رشته‌های بومی‌سازی شده از فایل‌های .resx.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager Rm = new(
        "AIXray.App.Resources.Strings",
        typeof(Strings).Assembly);

    public static string Get(string key) => Rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public static string AppTitle => Get("AppTitle");
    public static string Menu_File => Get("Menu_File");
    public static string Menu_ImportFromFile => Get("Menu_ImportFromFile");
    public static string Menu_ImportFromClipboard => Get("Menu_ImportFromClipboard");
    public static string Menu_CustomConfig => Get("Menu_CustomConfig");
    public static string Menu_Groups => Get("Menu_Groups");
    public static string Menu_CreateGroup => Get("Menu_CreateGroup");
    public static string Menu_Settings => Get("Menu_Settings");
    public static string Groups_Title => Get("Groups_Title");
    public static string Groups_Create => Get("Groups_Create");
    public static string Servers_Title => Get("Servers_Title");
    public static string BottomBar_AutoConnect => Get("BottomBar_AutoConnect");
    public static string BottomBar_StatusReady => Get("BottomBar_StatusReady");
    public static string BottomBar_Connected => Get("BottomBar_Connected");
    public static string BottomBar_Disconnected => Get("BottomBar_Disconnected");
    public static string Dialog_Cancel => Get("Dialog_Cancel");
    public static string Dialog_Save => Get("Dialog_Save");
    public static string Dialog_Add => Get("Dialog_Add");
    public static string Status_Connecting => Get("Status_Connecting");
    public static string Status_NoServerSelected => Get("Status_NoServerSelected");
    public static string Status_NoValidLinks => Get("Status_NoValidLinks");
}
