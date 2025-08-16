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

public partial class McpServersView : UserControl, IRecipient<ShowMcpServerDetailMessage>
{
    public McpServersView()
    {
        InitializeComponent();
        
        WeakReferenceMessenger.Default.Register<ShowMcpServerDetailMessage>(this);
    }

    public void Receive(ShowMcpServerDetailMessage detailMessage)
    {
        Dispatcher.UIThread.Post(async void () =>
        {
            try
            {
                var notificationService = Ioc.Default.GetService<INotificationService>()!;
                var configurationService = Ioc.Default.GetService<IConfigurationService>()!;
                var mcpServerViewModel = new McpServerViewViewModel(detailMessage.McpServer, notificationService, configurationService);

                mcpServerViewModel.IsActive = true;

                var options = new DrawerOptions()
                {
                    Position = Position.Right,
                    Buttons = DialogButton.None,
                    CanLightDismiss = false,
                    IsCloseButtonVisible = false,
                    CanResize = false
                };

                var result = await Drawer.ShowCustomModal<McpServerView, McpServerViewViewModel, object?>(
                    mcpServerViewModel, options: options);

                mcpServerViewModel.IsActive = false;

                if (result is CloseResult closeResult && closeResult != CloseResult.Cancelled)
                {
                    if (DataContext is McpServersViewViewModel mcpServersViewModel)
                    {
                        await mcpServersViewModel.RefreshMcpServersAsync();
                    }
                }
            }
            catch (Exception e)
            {
                // TODO handle exception
            }
        });
    }
}