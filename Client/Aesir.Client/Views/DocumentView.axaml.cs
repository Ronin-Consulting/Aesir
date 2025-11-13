using System.Linq;
using Aesir.Client.Messages;
using Aesir.Client.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using CommunityToolkit.Mvvm.Messaging;

namespace Aesir.Client.Views;

public partial class DocumentView : UserControl
{
    public DocumentView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // modal view is cached by drawer framework, so reset any stale validation errors
        foreach (var ctl in this.GetSelfAndLogicalDescendants().OfType<Control>())
            DataValidationErrors.ClearErrors(ctl);

    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not DocumentViewViewModel viewModel) return;

        WeakReferenceMessenger.Default.Send(new FileDownloadRequestMessage()
        {
            FileName = viewModel.FormModel.Path
        });
    }
}