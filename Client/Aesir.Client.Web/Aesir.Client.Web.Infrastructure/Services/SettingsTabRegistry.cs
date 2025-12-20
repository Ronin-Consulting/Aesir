namespace Aesir.Client.Web.Infrastructure.Services;

/// <summary>
/// Implementation of ISettingsTabRegistry that manages dynamic settings tabs.
/// </summary>
public class SettingsTabRegistry : ISettingsTabRegistry
{
    private readonly List<SettingsTabDefinition> _tabs = [];

    /// <inheritdoc />
    public IReadOnlyList<SettingsTabDefinition> GetTabs()
    {
        return _tabs.OrderBy(t => t.Priority).ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public void RegisterTab(SettingsTabDefinition tab)
    {
        ArgumentNullException.ThrowIfNull(tab);

        // Remove existing tab with same ID if present (allows re-registration)
        _tabs.RemoveAll(t => t.TabId == tab.TabId);
        _tabs.Add(tab);
    }

    /// <inheritdoc />
    public SettingsTabDefinition? GetTab(string tabId)
    {
        return _tabs.FirstOrDefault(t => t.TabId == tabId);
    }
}
