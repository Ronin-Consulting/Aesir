using System;
using Aesir.Client.Controls;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Ursa.Controls;

namespace Aesir.Client.Views;

public partial class LogsView : UserControl, IDisposable, IRecipient<ShowLogDetailMessage>
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
                var model = new LogDetailDialogViewModel();
                model.Log = detailMessage.Log;
        
                await OverlayDialog.ShowModal<LogDetailDialog, LogDetailDialogViewModel>(
                    model,
                    options: new OverlayDialogOptions()
                    {
                        Title = "Log Detail View",
                        Mode = DialogMode.Info,
                        Buttons = DialogButton.OK,
                        CanLightDismiss = true
                    }
                );
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
        if (dataGridCellPointerPressedEventArgs.Row.DataContext is AesirKernelLog log)
        {
            WeakReferenceMessenger.Default.Send(new ShowLogDetailMessage(log));
        }
    }
}