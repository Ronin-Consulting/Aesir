using Microsoft.JSInterop;
using MudBlazor;

namespace Aesir.Client.Web.Modules.Wizard.Services;

/// <summary>
/// Implementation of IWizardStateService that manages wizard state.
/// </summary>
public class WizardStateService : IWizardStateService
{
    private readonly IJSRuntime _jsRuntime;
    private const string WizardCompletedKey = "aesir_wizard_completed";

    private int _currentStep;
    private bool _isCompleted;
    private readonly List<WizardStepDefinition> _steps;

    public WizardStateService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _steps =
        [
            new WizardStepDefinition
            {
                Id = "welcome",
                Title = "Welcome",
                Icon = Icons.Material.Filled.Home,
                IsOptional = false
            },
            new WizardStepDefinition
            {
                Id = "inference-engine",
                Title = "Inference Engine",
                Icon = Icons.Material.Filled.Memory,
                IsOptional = false
            },
            new WizardStepDefinition
            {
                Id = "general-settings",
                Title = "General Settings",
                Icon = Icons.Material.Filled.Settings,
                IsOptional = false
            },
            new WizardStepDefinition
            {
                Id = "agent",
                Title = "Agent",
                Icon = Icons.Material.Filled.SmartToy,
                IsOptional = false
            },
            new WizardStepDefinition
            {
                Id = "complete",
                Title = "Complete",
                Icon = Icons.Material.Filled.CheckCircle,
                IsOptional = false
            }
        ];
    }

    /// <inheritdoc />
    public int CurrentStep => _currentStep;

    /// <inheritdoc />
    public bool IsCompleted => _isCompleted;

    /// <inheritdoc />
    public IReadOnlyList<WizardStepDefinition> Steps => _steps;

    /// <inheritdoc />
    public event EventHandler? StateChanged;

    /// <inheritdoc />
    public Task<bool> GoToNextStepAsync()
    {
        if (_currentStep < _steps.Count - 1)
        {
            _currentStep++;
            OnStateChanged();
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public void GoToPreviousStep()
    {
        if (_currentStep > 0)
        {
            _currentStep--;
            OnStateChanged();
        }
    }

    /// <inheritdoc />
    public void GoToStep(int index)
    {
        if (index >= 0 && index < _steps.Count)
        {
            _currentStep = index;
            OnStateChanged();
        }
    }

    /// <inheritdoc />
    public void CompleteStep(int index)
    {
        if (index >= 0 && index < _steps.Count)
        {
            _steps[index].IsCompleted = true;
            OnStateChanged();
        }
    }

    /// <inheritdoc />
    public async Task CompleteWizardAsync()
    {
        _isCompleted = true;
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", WizardCompletedKey, "true");
        OnStateChanged();
    }

    /// <inheritdoc />
    public async Task<bool> CheckWizardCompletedAsync()
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", WizardCompletedKey);
            _isCompleted = result == "true";
            return _isCompleted;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task ResetWizardAsync()
    {
        _currentStep = 0;
        _isCompleted = false;
        foreach (var step in _steps)
        {
            step.IsCompleted = false;
        }
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", WizardCompletedKey);
        OnStateChanged();
    }

    private void OnStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
