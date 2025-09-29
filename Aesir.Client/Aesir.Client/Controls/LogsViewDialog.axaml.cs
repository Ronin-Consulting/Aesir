using System;
using Aesir.Client.Messages;
using Aesir.Client.ViewModels;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Ursa.Controls;

namespace Aesir.Client.Controls;

public partial class LogsViewDialog : UserControl, IDisposable, IRecipient<ShowLogDetailMessage>
{
    public LogsViewDialog()
    {
        InitializeComponent();
        
        WeakReferenceMessenger.Default.Register<ShowLogDetailMessage>(this);
    }

    public void Receive(ShowLogDetailMessage message)
    {
        Dispatcher.UIThread.Post(async void () =>
        {
            try
            {
                var model = new LogDetailDialogViewModel();
                model.Log = message.Log;
        
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

    private void InputElement_OnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        var control = sender as Control;
        if (control != null)
        {
            var model = control.DataContext as LogsViewDialogViewModel; // your clicked item
            if (model?.SelectedLog!=null)
            {
                WeakReferenceMessenger.Default.Send(new ShowLogDetailMessage(model.SelectedLog));
            }
        }
    }
}