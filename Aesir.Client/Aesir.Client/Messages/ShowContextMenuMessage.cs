using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace Aesir.Client.Messages
{
    public class ShowContextMenuMessage : ValueChangedMessage<(Guid? ChatSessionId, string Title)>
    {
        public ShowContextMenuMessage(Guid? chatSessionId, string title) 
            : base((chatSessionId, title))
        {
        }
    }
}