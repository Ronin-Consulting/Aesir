using System;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using Aesir.Client.Shared;
using Aesir.Client.ViewModels;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Ursa.Common;
using Ursa.Controls;
using Ursa.Controls.Options;

namespace Aesir.Client.Views;

public partial class ToolsView : UserControl, IRecipient<ShowToolDetailMessage>, IDisposable
{
    public ToolsView()
    {
        InitializeComponent();
        
        WeakReferenceMessenger.Default.Register<ShowToolDetailMessage>(this);
    }

    public void Receive(ShowToolDetailMessage detailMessage)
    {
        Dispatcher.UIThread.Post(async void () =>
        {
            try
            {
                var notificationService = Ioc.Default.GetService<INotificationService>()!;
                var configurationService = Ioc.Default.GetService<IConfigurationService>()!;
                var viewModel = new ToolViewViewModel(detailMessage.Tool, notificationService, configurationService)
                    {
                        IsActive = true
                    };

                var options = new DrawerOptions()
                {
                    Position = Position.Right,
                    Buttons = DialogButton.None,
                    CanLightDismiss = false,
                    IsCloseButtonVisible = false,
                    CanResize = false
                };

                var result = await Drawer.ShowCustomModal<ToolView, ToolViewViewModel, object?>(
                    viewModel, options: options);

                viewModel.IsActive = false;

                if (DataContext is ToolsViewViewModel toolsViewModel)
                {
                    toolsViewModel.SelectedTool = null;
                    
                    if (result is CloseResult closeResult && closeResult != CloseResult.Cancelled)
                    {
                        await toolsViewModel.RefreshToolsAsync();
                    }
                }
            }
            catch (Exception e)
            {
                // TODO handle exception
            }
        });
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Dispose();
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<ShowToolDetailMessage>(this);
    }
}