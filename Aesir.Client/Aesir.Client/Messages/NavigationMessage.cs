namespace Aesir.Client.Messages;

public class NavigationMessage
{
    public enum ViewType
    {
        Chat,
        Tools,
        Agents
    }
    
    public ViewType View { get; set; }

    public NavigationMessage(ViewType view)
    {
        View = view;;
    }
}