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

public partial class AgentsView : UserControl, IRecipient<ShowAgentDetailMessage>
{   
    public AgentsView()
    {
        InitializeComponent();
        
        WeakReferenceMessenger.Default.Register<ShowAgentDetailMessage>(this);
    }

    public void Receive(ShowAgentDetailMessage detailMessage)
    {
        Dispatcher.UIThread.Post(async void () =>
        {
            try
            {
                var notificationService = Ioc.Default.GetService<INotificationService>()!;
                var viewModel = new AgentViewViewModel(detailMessage.Agent, notificationService);

                var options = new DrawerOptions()
                {
                    Position = Position.Right,
                    Buttons = DialogButton.None,
                    CanLightDismiss = false,
                    IsCloseButtonVisible = false,
                    CanResize = false
                };

                var result = await Drawer.ShowCustomModal<AgentView, AgentViewViewModel, object?>(
                    viewModel, options: options);

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