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

public partial class InferenceEnginesView : UserControl, IRecipient<ShowInferenceEngineDetailMessage>, IDisposable
{   
    public InferenceEnginesView()
    {
        InitializeComponent();
        
        WeakReferenceMessenger.Default.Register<ShowInferenceEngineDetailMessage>(this);
    }

    public void Receive(ShowInferenceEngineDetailMessage detailMessage)
    {
        Dispatcher.UIThread.Post(async void () =>
        {
            try
            {
                var notificationService = Ioc.Default.GetService<INotificationService>()!;
                var configurationService = Ioc.Default.GetService<IConfigurationService>()!;
                var viewModel = new InferenceEngineViewViewModel(detailMessage.InferenceEngine, notificationService, configurationService);

                viewModel.IsActive = true;

                var options = new DrawerOptions()
                {
                    Position = Position.Right,
                    Buttons = DialogButton.None,
                    CanLightDismiss = false,
                    IsCloseButtonVisible = false,
                    CanResize = false
                };

                var result = await Drawer.ShowCustomModal<InferenceEngineView, InferenceEngineViewViewModel, object?>(
                    viewModel, options: options);

                viewModel.IsActive = false;

                if (result is CloseResult closeResult && closeResult != CloseResult.Cancelled)
                {
                    if (DataContext is InferenceEnginesViewViewModel inferenceEnginesViewModel)
                    {
                        await inferenceEnginesViewModel.RefreshInferenceEnginesAsync();
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
        WeakReferenceMessenger.Default.Unregister<ShowInferenceEngineDetailMessage>(this);
    }
}