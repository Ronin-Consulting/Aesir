using System;
using System.Globalization;
using System.Threading.Tasks;
using Aesir.Client.Controls;
using Aesir.Client.ViewModels;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CommunityToolkit.Mvvm.DependencyInjection;
using Ursa.Controls;

namespace Aesir.Client.Views;

public partial class AesirSplashWindow : SplashWindow
{
    public AesirSplashWindow()
    {
        InitializeComponent();
    }
    
    protected override async Task<Window?> CreateNextWindow()
    {
        if (DialogResult is not true) return null;
        
        var model = Ioc.Default.GetRequiredService<MainWindowViewModel>();
        return new MainWindow().WithViewModel(model);
    }
}

public class BooleanToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isError)
        {
            return isError ? Brushes.Red : Brushes.White;
        }
        return Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
