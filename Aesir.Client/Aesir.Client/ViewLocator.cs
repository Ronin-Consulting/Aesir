using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aesir.Client;

/// <summary>
/// Represents a utility class responsible for mapping ViewModels to Views within the Avalonia UI framework.
/// </summary>
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// Builds and returns a control instance based on the given data object's type.
    /// Determines the control type by resolving the view name associated with the data object's type.
    /// </summary>
    /// <param name="data">The data object used to determine the type of the control to build. Typically a ViewModel object.</param>
    /// <returns>A control corresponding to the resolved view type based on the data object's type. Returns a TextBlock with an error message if the view cannot be found.</returns>
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

    /// Determines whether the specified data matches the expected type criteria.
    /// <param name="data">The data object to check.</param>
    /// <return>Returns true if the data object is of type ObservableRecipient; otherwise, false.</return>
    public bool Match(object? data)
    {
        return data is ObservableRecipient;
    }
}