using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace Aesir.Client.Messages
{
    public class ShowContextMenuMessage(Guid? chatSessionId, string title)
        : ValueChangedMessage<(Guid? ChatSessionId, string Title)>((chatSessionId, title));
}