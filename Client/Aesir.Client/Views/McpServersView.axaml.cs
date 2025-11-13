using System;
using System.Threading.Tasks;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using Aesir.Client.Shared;
using Aesir.Client.ViewModels;
using Aesir.Common.Models;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Ursa.Common;
using Ursa.Controls;
using Ursa.Controls.Options;

namespace Aesir.Client.Views;

public partial class McpServersView : UserControl, IRecipient<ShowMcpServerDetailMessage>, IDisposable
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
                var dialogService = Ioc.Default.GetService<IDialogService>()!;
                var configurationService = Ioc.Default.GetService<IConfigurationService>()!;
                
                if (detailMessage.Import)
                {
                    var mcpServerImportViewModel = new McpServerImportViewViewModel(notificationService, configurationService);
                    
                    var options = new OverlayDialogOptions()
                    {
                        FullScreen = false,
                        HorizontalAnchor = HorizontalPosition.Center,
                        VerticalAnchor = VerticalPosition.Center,
                        Mode = DialogMode.None,
                        Buttons = DialogButton.None,
                        Title = "Import MCP Server",
                        CanLightDismiss = false,
                        CanDragMove = true,
                        IsCloseButtonVisible = true,
                        CanResize = false
                    };
                    
                    var dialogResult = await OverlayDialog.ShowModal<McpServerImportView, McpServerImportViewViewModel>(mcpServerImportViewModel, options: options);
                    
                    if (dialogResult == DialogResult.OK)
                    {
                        await ShowMcpServerView(mcpServerImportViewModel.GeneratedMcpServer, notificationService, dialogService, configurationService);
                    }
                }
                else
                {
                    await ShowMcpServerView(detailMessage.McpServer, notificationService, dialogService, configurationService);   
                }
            }
            catch (Exception e)
            {
                // TODO handle exception
            }
        });
    }

    private async Task ShowMcpServerView(AesirMcpServerBase mcpServer, INotificationService notificationService, IDialogService dialogService, IConfigurationService configurationService)
    {
        var mcpServerViewModel = new McpServerViewViewModel(mcpServer, notificationService, dialogService, configurationService)
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

        var result = await Drawer.ShowCustomModal<McpServerView, McpServerViewViewModel, object?>(
            mcpServerViewModel, options: options);

        mcpServerViewModel.IsActive = false;

        if (DataContext is McpServersViewViewModel mcpServersViewModel)
        {
            mcpServersViewModel.SelectedMcpServer = null;
                    
            if (result is CloseResult closeResult && closeResult != CloseResult.Cancelled)
            {
                await mcpServersViewModel.RefreshMcpServersAsync();
            }
        }
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Dispose();
    }

    public void Dispose()
    {
        WeakReferenceMessenger.Default.Unregister<ShowMcpServerDetailMessage>(this);
    }
}