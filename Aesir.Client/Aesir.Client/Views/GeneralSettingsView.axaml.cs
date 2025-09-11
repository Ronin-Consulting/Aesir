using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace Aesir.Client.Views;

public partial class GeneralSettingsView : UserControl
{
    public GeneralSettingsView()
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
}