namespace Aesir.Client.Web.Infrastructure.Modules;

/// <summary>
/// Registry for module navigation items.
/// Modules register their navigation during startup, and the UI consumes this to build menus.
/// </summary>
public interface INavigationRegistry
{
    /// <summary>
    /// Registers a navigation item.
    /// </summary>
    /// <param name="item">The navigation item to register.</param>
    void Register(NavigationItem item);

    /// <summary>
    /// Gets all registered navigation items ordered by priority.
    /// </summary>
    /// <returns>An enumerable of navigation items.</returns>
    IEnumerable<NavigationItem> GetItems();
}

/// <summary>
/// Represents a navigation item in the application menu.
/// </summary>
public record NavigationItem
{
    /// <summary>
    /// Gets the display text for the navigation item.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the route/href for the navigation item.
    /// </summary>
    public required string Href { get; init; }

    /// <summary>
    /// Gets the Material Design icon name (e.g., "Chat", "Settings").
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets the display order priority (lower values appear first).
    /// </summary>
    public int Priority { get; init; } = 100;

    /// <summary>
    /// Gets the group this item belongs to (for grouping in menus).
    /// </summary>
    public string? Group { get; init; }
}
