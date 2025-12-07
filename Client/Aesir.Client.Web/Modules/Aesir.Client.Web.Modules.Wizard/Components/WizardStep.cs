using Microsoft.AspNetCore.Components;

namespace Aesir.Client.Web.Modules.Wizard.Components;

/// <summary>
/// Represents a step in the wizard.
/// </summary>
public class WizardStep
{
    /// <summary>
    /// The unique identifier for this step.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The title displayed for this step.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Optional icon for the step.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// The content to render for this step.
    /// </summary>
    public RenderFragment? Content { get; set; }

    /// <summary>
    /// Whether this step is optional and can be skipped.
    /// </summary>
    public bool IsOptional { get; init; }

    /// <summary>
    /// Whether this step has been completed.
    /// </summary>
    public bool IsCompleted { get; set; }
}
