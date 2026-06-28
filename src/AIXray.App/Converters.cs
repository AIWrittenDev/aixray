using System.Globalization;
using System.Windows.Data;
using AIXray.Core;
using Wpf.Ui.Controls;

namespace AIXray.App;

public class ConnectButtonConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "Danger" : "Primary";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class ConnectIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? SymbolRegular.Stop24 : SymbolRegular.Play24;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

public class ConnectionModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ConnectionMode mode)
        {
            return mode switch
            {
                ConnectionMode.SystemProxy => 0,
                ConnectionMode.Tun => 1,
                ConnectionMode.Direct => 2,
                _ => 0,
            };
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int idx)
        {
            return idx switch
            {
                0 => ConnectionMode.SystemProxy,
                1 => ConnectionMode.Tun,
                2 => ConnectionMode.Direct,
                _ => ConnectionMode.SystemProxy,
            };
        }
        return ConnectionMode.SystemProxy;
    }
}
