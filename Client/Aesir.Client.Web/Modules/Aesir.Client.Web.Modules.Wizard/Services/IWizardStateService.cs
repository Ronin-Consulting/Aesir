namespace Aesir.Client.Web.Modules.Wizard.Services;

/// <summary>
/// Service interface for managing wizard state.
/// </summary>
public interface IWizardStateService
{
    /// <summary>
    /// Gets the current step index.
    /// </summary>
    int CurrentStep { get; }

    /// <summary>
    /// Gets whether the wizard has been completed.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Gets the list of wizard step definitions.
    /// </summary>
    IReadOnlyList<WizardStepDefinition> Steps { get; }

    /// <summary>
    /// Event raised when the wizard state changes.
    /// </summary>
    event EventHandler? StateChanged;

    /// <summary>
    /// Navigate to the next step.
    /// </summary>
    Task<bool> GoToNextStepAsync();

    /// <summary>
    /// Navigate to the previous step.
    /// </summary>
    void GoToPreviousStep();

    /// <summary>
    /// Navigate to a specific step by index.
    /// </summary>
    void GoToStep(int index);

    /// <summary>
    /// Mark a step as completed.
    /// </summary>
    void CompleteStep(int index);

    /// <summary>
    /// Mark the wizard as completed and persist the completion state.
    /// </summary>
    Task CompleteWizardAsync();

    /// <summary>
    /// Check if the wizard has been previously completed (from localStorage).
    /// </summary>
    Task<bool> CheckWizardCompletedAsync();

    /// <summary>
    /// Reset the wizard state to start fresh.
    /// </summary>
    Task ResetWizardAsync();
}

/// <summary>
/// Defines a step in the setup wizard.
/// </summary>
public class WizardStepDefinition
{
    /// <summary>
    /// The unique identifier for this step.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The display title for this step.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The icon to display for this step.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Whether this step is optional and can be skipped.
    /// </summary>
    public bool IsOptional { get; init; }

    /// <summary>
    /// Whether this step has been completed.
    /// </summary>
    public bool IsCompleted { get; set; }
}
