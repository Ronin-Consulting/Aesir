using System;
using System.IO;
using System.Threading.Tasks;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Client.Shared;
using Aesir.Client.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Ursa.Common;
using Ursa.Controls;
using Ursa.Controls.Options;

namespace Aesir.Client.Views;

public partial class LogsView : UserControl, IRecipient<ShowLogDetailMessage>, IDisposable
{
    public LogsView()
    {
        InitializeComponent();
        
        WeakReferenceMessenger.Default.Register<ShowLogDetailMessage>(this);
    }

    public void Receive(ShowLogDetailMessage detailMessage)
    {
        Dispatcher.UIThread.Post(async void () =>
        {
            try
            {
                // var notificationService = Ioc.Default.GetService<INotificationService>()!;
                // var LogCollectionService = Ioc.Default.GetService<ILogCollectionService>()!;
                // var chatHistoryService=Ioc.Default.GetService<IChatHistoryService>()!;
                // var viewModel = new LogViewViewModel(detailMessage.Log, notificationService, LogCollectionService, chatHistoryService)
                //     {
                //         IsActive = true
                //     };
                //
                // var options = new DrawerOptions()
                // {
                //     Position = Position.Right,
                //     Buttons = DialogButton.None,
                //     CanLightDismiss = false,
                //     IsCloseButtonVisible = false,
                //     CanResize = false
                // };
                //
                // var result = await Drawer.ShowCustomModal<LogView, LogViewViewModel, object?>(
                //     viewModel, options: options);
                //
                // viewModel.IsActive = false;
                //
                // if (result is CloseResult closeResult && closeResult != CloseResult.Cancelled)
                // {
                //     if (DataContext is LogsViewViewModel LogsViewModel)
                //     {
                //         await LogsViewModel.RefreshLogsAsync();
                //     }
                // }
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
        WeakReferenceMessenger.Default.Unregister<ShowLogDetailMessage>(this);
    }
    
    private void InputElement_OnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs dataGridCellPointerPressedEventArgs)
    {
        // sender is often the clicked UI element, like ListBoxItem
        var control = sender as Control;
        if (control != null)
        {
            var model = control.DataContext as LogsViewViewModel; // your clicked item
            if (model?.SelectedLog!=null)
            {
                WeakReferenceMessenger.Default.Send(new ShowLogDetailMessage(model.SelectedLog));
            }
        }
    }



}