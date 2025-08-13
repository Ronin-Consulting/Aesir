using System;
using Aesir.Client.Messages;
using Aesir.Client.Services;
using Aesir.Client.ViewModels;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Ursa.Common;
using Ursa.Controls;
using Ursa.Controls.Options;

namespace Aesir.Client.Views;

public partial class ToolsView : UserControl, IRecipient<ShowToolDetailMessage>
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
                var viewModel = new ToolViewViewModel(detailMessage.Tool, notificationService, configurationService);

                viewModel.IsActive = true;

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

                //if (result == DialogResult.None)
                //    throw new ApplicationException("PdfViewerService.ShowPdfAsync failed.");
            }
            catch (Exception e)
            {
                // TODO handle exception
            }
        });
    }
}