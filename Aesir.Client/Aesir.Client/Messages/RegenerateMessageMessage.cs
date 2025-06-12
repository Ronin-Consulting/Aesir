using Aesir.Client.ViewModels;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Aesir.Client.Messages;

public class RegenerateMessageMessage : ValueChangedMessage<MessageViewModel>
{
    public RegenerateMessageMessage(MessageViewModel value) : base(value)
    {
    }
}