using System;
using Avalonia.Controls;
using System.Threading.Tasks;
using Aesir.Client.Desktop.ViewModels;
using Avalonia.Controls.PanAndZoom;
using MsBox.Avalonia.Base;

namespace Aesir.Client.Desktop.Controls;

public partial class PdfViewerControl : UserControl, IFullApi<string>, ISetCloseAction
{
    private string? _buttonResult;
    private Action? _closeAction;
    
    public PdfViewerControl()
    {
        InitializeComponent();
    }
    
    public void SetButtonResult(string bdName)
    {
        _buttonResult = bdName;
    }

    public string GetButtonResult()
    {
        return _buttonResult ??  string.Empty;
    }

    public Task Copy()
    {
        // var clipboard = TopLevel.GetTopLevel(this).Clipboard;
        // var text = ContentTextBox.SelectedText;
        // if (string.IsNullOrEmpty(text))
        // {
        //     text = (DataContext as AbstractMsBoxViewModel)?.ContentMessage;
        // }
        // return clipboard?.SetTextAsync(text);
        
        return Task.CompletedTask;
    }

    public void Close()
    {
        _closeAction?.Invoke();
    }

    public void CloseWindow(object sender, EventArgs eventArgs)
    {
        ((IClose)this).Close();
    }
    
    public void SetCloseAction(Action closeAction)
    {
        _closeAction = closeAction;
    }
}

public class ZoomApiImpl(ZoomBorder zoomBorder) : IZoomApi
{
    public void DecrementZoom()
    {
        zoomBorder.ZoomOut();
    }
    
    public void IncrementZoom()
    {
        zoomBorder.ZoomIn();
    }
}