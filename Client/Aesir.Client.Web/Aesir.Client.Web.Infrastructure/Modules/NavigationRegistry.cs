namespace Aesir.Client.Web.Infrastructure.Modules;

/// <summary>
/// Default implementation of the navigation registry.
/// </summary>
public class NavigationRegistry : INavigationRegistry
{
    private readonly List<NavigationItem> _items = [];

    /// <inheritdoc />
    public void Register(NavigationItem item)
    {
        _items.Add(item);
    }

    /// <inheritdoc />
    public IEnumerable<NavigationItem> GetItems()
    {
        return _items.OrderBy(x => x.Priority).ThenBy(x => x.Title);
    }
}
