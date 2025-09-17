using System;
using Aesir.Client.Messages;
using Aesir.Client.Models;
using Aesir.Client.Services;
using Aesir.Client.Shared;
using Aesir.Client.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Ursa.Common;
using Ursa.Controls;
using Ursa.Controls.Options;

namespace Aesir.Client.Views;

public partial class DocumentsView : UserControl, IRecipient<ShowDocumentDetailMessage>, IDisposable
{
    public DocumentsView()
    {
        InitializeComponent();
        
        WeakReferenceMessenger.Default.Register<ShowDocumentDetailMessage>(this);
    }

    public void Receive(ShowDocumentDetailMessage detailMessage)
    {
        Dispatcher.UIThread.Post(async void () =>
        {
            try
            {
                var notificationService = Ioc.Default.GetService<INotificationService>()!;
                var documentCollectionService = Ioc.Default.GetService<IDocumentCollectionService>()!;
                var viewModel = new DocumentViewViewModel(detailMessage.Document, notificationService, documentCollectionService)
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

                var result = await Drawer.ShowCustomModal<DocumentView, DocumentViewViewModel, object?>(
                    viewModel, options: options);

                viewModel.IsActive = false;

                if (result is CloseResult closeResult && closeResult != CloseResult.Cancelled)
                {
                    if (DataContext is DocumentsViewViewModel documentsViewModel)
                    {
                        await documentsViewModel.RefreshDocumentsAsync();
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
    
    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // sender is often the clicked UI element, like ListBoxItem
        var control = sender as Control;
        if (control!= null)
        {
            var selectedItem = control.DataContext as AesirDocument; // your clicked item
            // Use selectedItem here
            WeakReferenceMessenger.Default.Send(new ShowDocumentDetailMessage(selectedItem));
        }
    }
}