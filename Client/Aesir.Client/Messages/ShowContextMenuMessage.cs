using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace Aesir.Client.Messages
{
    /// <summary>
    /// Represents a message used to indicate that a context menu should be displayed.
    /// </summary>
    /// <remarks>
    /// This message contains data about the context menu such as the associated
    /// chat session identifier and the title to display.
    /// </remarks>
    public class ShowContextMenuMessage(Guid? chatSessionId, string title)
        : ValueChangedMessage<(Guid? ChatSessionId, string Title)>((chatSessionId, title));
}