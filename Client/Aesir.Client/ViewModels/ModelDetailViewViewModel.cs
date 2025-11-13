using System;
using System.Linq;
using Aesir.Common.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Avalonia.Shared.Contracts;

namespace Aesir.Client.ViewModels;

public partial class ModelDetailViewViewModel : ObservableRecipient, IDialogContext
{
    [ObservableProperty]
    private string? _parentModel;
    
    [ObservableProperty]
    private string? _family;
    
    [ObservableProperty]
    private string? _families;
    
    [ObservableProperty]
    private string? _format;
   
    [ObservableProperty]
    private string? _parameterSize;
    
    [ObservableProperty]
    private string? _quantizationLevel;
    
    [ObservableProperty]
    private string? _capabilities;
    
    [ObservableProperty]
    private string? _extraInfo;
    
    [ObservableProperty]
    private string? _license;
    
    public ModelDetailViewViewModel(AesirModelDetails? details)
    {
        ParentModel = string.IsNullOrEmpty(details?.ParentModel) ? "N/A" : details.ParentModel;
        Family = string.IsNullOrEmpty(details?.Family) ? "N/A" : details.Family;
        
        Families = "N/A";
        if (details?.Families is { Length: > 0 })
            Families = string.Join("\n", details.Families);
        
        Format = string.IsNullOrEmpty(details?.Format) ? "N/A" : details.Format;
        ParameterSize = string.IsNullOrEmpty(details?.ParameterSize) ? "N/A" : details.ParameterSize;
        QuantizationLevel = string.IsNullOrEmpty(details?.QuantizationLevel) ? "N/A" : details.QuantizationLevel;
        
        Capabilities = "N/A";
        if (details?.Capabilities is { Length: > 0 })
            Capabilities = string.Join("\n", details.Capabilities);
        
        ExtraInfo = "N/A";
        if (details?.ExtraInfo is { Count: > 0 })
            ExtraInfo = string.Join(",\n", 
                details.ExtraInfo.Select(x => $"{x.Key}: {x.Value}"));
        
        License = string.IsNullOrEmpty(details?.License) ? "N/A" : details.License;
    }

    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }

    public event EventHandler<object?>? RequestClose;
}