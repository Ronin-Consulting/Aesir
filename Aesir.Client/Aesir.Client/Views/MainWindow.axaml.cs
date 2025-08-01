
using System;
using System.Threading.Tasks;
using Avalonia.Platform;
using Ursa.Controls;

namespace Aesir.Client.Views;

public partial class MainWindow : UrsaWindow
{
    public WindowNotificationManager? NotificationManager { get; set; }
    
    public MainWindow()
    {
        InitializeComponent();
        
        NotificationManager = new WindowNotificationManager(this) { MaxItems = 3 };

        // this is the most compatible approach;
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
        ExtendClientAreaToDecorationsHint = false;
        
        if (OperatingSystem.IsWindows() ||  OperatingSystem.IsLinux())
        {
            IsFullScreenButtonVisible = false;
            IsManagedResizerVisible = false;
            IsCloseButtonVisible = false;
            IsMinimizeButtonVisible = false;
            IsRestoreButtonVisible = false;
        }
        
        if (OperatingSystem.IsLinux())
        {
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.Default;
            ExtendClientAreaToDecorationsHint = false;
            
            IsFullScreenButtonVisible = false;
            IsManagedResizerVisible = false;
            IsCloseButtonVisible = true;
            IsMinimizeButtonVisible = false;
            IsRestoreButtonVisible = false;
        }
    }
    
    protected override async Task<bool> CanClose()
    {
        //var result = await MessageBox.ShowOverlayAsync("Are you sure you want to exit?\n您确定要退出吗？", "Exit", button: MessageBoxButton.YesNo);
        //return result == MessageBoxResult.Yes;

        return true;
    }
}