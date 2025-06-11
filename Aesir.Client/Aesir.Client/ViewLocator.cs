using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aesir.Client;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        var name = data.GetType().Name!.Replace("ViewModel", "", StringComparison.Ordinal);
        
        var type = Type.GetType("Aesir.Client.Controls."+name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }
        
        type = Type.GetType("Aesir.Client.Views."+name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ObservableRecipient;
    }
}