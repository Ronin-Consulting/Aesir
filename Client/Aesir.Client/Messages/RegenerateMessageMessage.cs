using Aesir.Client.ViewModels;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Aesir.Client.Messages;

/// <summary>
/// A message that represents a request to regenerate a specific message.
/// </summary>
/// <remarks>
/// This message is used within the context of the messaging framework to notify subscribers
/// when a specific message represented by a <see cref="MessageViewModel"/> instance
/// needs to be regenerated.
/// </remarks>
/// <param name="value">
/// The <see cref="MessageViewModel"/> instance that corresponds to the message
/// to be regenerated.
/// </param>
public class RegenerateMessageMessage(MessageViewModel value) : ValueChangedMessage<MessageViewModel>(value);
