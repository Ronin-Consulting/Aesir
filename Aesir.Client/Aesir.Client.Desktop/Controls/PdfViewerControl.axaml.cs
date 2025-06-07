using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia.Base;

namespace Aesir.Client.Desktop.Controls;

public partial class PdfViewerControl : UserControl, IFullApi<string>, ISetCloseAction
{
    private string _buttonResult;
    private Action _closeAction;
    
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
        return _buttonResult;
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