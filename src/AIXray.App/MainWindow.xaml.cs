using System.Windows;
using System.Windows.Data;
using System.Globalization;
using AIXray.Core;
using Wpf.Ui.Controls;

namespace AIXray.App;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();

        if (App.Services != null)
        {
            DataContext = App.Services.GetService(typeof(MainViewModel));
        }

        Loaded += async (_, _) =>
        {
        if (DataContext is MainViewModel vm)
        {
            _ = vm.InitializeCommand.ExecuteAsync(null);
        }
        };
    }
}

public class BoolToAppearanceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "Danger" : "Primary";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class BoolToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? Wpf.Ui.Controls.SymbolRegular.Dismiss24 : Wpf.Ui.Controls.SymbolRegular.Play24;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class ConnectButtonConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "Danger" : "Primary";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class ConnectToggleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is MainWindow window && window.DataContext is MainViewModel vm)
        {
            return value is true
                ? vm.DisconnectCommand
                : vm.ConnectCommand;
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class ConnectIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true
            ? Wpf.Ui.Controls.SymbolRegular.Stop24
            : Wpf.Ui.Controls.SymbolRegular.Play24;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
