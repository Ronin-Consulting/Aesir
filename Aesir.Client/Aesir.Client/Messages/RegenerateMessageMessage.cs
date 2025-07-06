using Aesir.Client.ViewModels;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Aesir.Client.Messages;

public class RegenerateMessageMessage(MessageViewModel value) : ValueChangedMessage<MessageViewModel>(value);
