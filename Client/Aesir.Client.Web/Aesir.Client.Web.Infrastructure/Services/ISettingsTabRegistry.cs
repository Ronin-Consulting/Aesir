namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Defines a tab that can be registered with the Settings page.
/// </summary>
public record SettingsTabDefinition
{
    /// <summary>
    /// Unique identifier for the tab.
    /// </summary>
    public required string TabId { get; init; }

    /// <summary>
    /// Display label for the tab.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Icon name for the tab (MudBlazor icon name).
    /// </summary>
    public required string Icon { get; init; }

    /// <summary>
    /// Priority for ordering (lower = earlier). Built-in tabs use 100, 200, etc.
    /// </summary>
    public int Priority { get; init; } = 500;

    /// <summary>
    /// The component type to render for this tab's content.
    /// </summary>
    public required Type ContentComponentType { get; init; }
}

/// <summary>
/// Registry for dynamic settings tabs. Modules can register their tabs
/// to appear in the Settings page without direct coupling.
/// </summary>
public interface ISettingsTabRegistry
{
    /// <summary>
    /// Gets all registered tabs, ordered by priority.
    /// </summary>
    IReadOnlyList<SettingsTabDefinition> GetTabs();

    /// <summary>
    /// Registers a new tab definition.
    /// </summary>
    /// <param name="tab">The tab definition to register.</param>
    void RegisterTab(SettingsTabDefinition tab);

    /// <summary>
    /// Gets a specific tab by its ID.
    /// </summary>
    /// <param name="tabId">The tab identifier.</param>
    /// <returns>The tab definition, or null if not found.</returns>
    SettingsTabDefinition? GetTab(string tabId);
}
