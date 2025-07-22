using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aesir.Client.Services.Implementations.NoOp;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aesir.Client.ViewModels.Design;

public class DesignAgentViewViewModel : AgentViewViewModel
{   
    public DesignAgentViewViewModel() : base(new AesirAgent()
    {
        // TODO
    })
    {
        AvailableTools = new ObservableCollection<string>()
        {
            "RAG",
            "Web"
        };
        SelectedTools = new ObservableCollection<string>();
        IsDirty = false;
        SaveCommand = new RelayCommand(() => { });
        CancelCommand = new RelayCommand(() => { });
        DeleteCommand = new RelayCommand(() => { });
    }
}