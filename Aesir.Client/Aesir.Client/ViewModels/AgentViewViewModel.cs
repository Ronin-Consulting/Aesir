using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;

namespace Aesir.Client.ViewModels;

public class AgentViewViewModel : ObservableRecipient, IDialogContext
{
    public AesirAgent Agent { get; set; }
    
    public ObservableCollection<string> AvailableTools { get; set; }
    
    public ObservableCollection<string> SelectedTools { get; set; }
    
    public bool IsDirty { get; set; }
    
    public ICommand SaveCommand { get; set; }
    
    public ICommand CancelCommand { get; set; }
    
    public ICommand DeleteCommand { get; set; }
    
    public event EventHandler<object?>? RequestClose;

    public AgentViewViewModel(AesirAgent agent)
    {
        Agent = agent;
        IsDirty = false;
        SaveCommand = new RelayCommand(ExecuteSaveCommand);
        CancelCommand = new RelayCommand(ExecuteCancelCommand);
        DeleteCommand = new RelayCommand(ExecuteDeleteCommand);
        
        // TODO actually load
        AvailableTools = new ObservableCollection<string>()
        {
            "RAG",
            "Web"
        };
        SelectedTools = new ObservableCollection<string>();
    }

    private void ExecuteSaveCommand()
    {
        // TODO - save, toast?
        Close();
    }

    private void ExecuteCancelCommand()
    {
        Close();
    }

    private void ExecuteDeleteCommand()
    {
        // TODO - delete, toast?
        Close();
    }

    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }
}