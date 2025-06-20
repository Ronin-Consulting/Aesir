using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Aesir.Client.ViewModels;

namespace Aesir.Client.Services;

public interface IChatSessionManager
{
    Task LoadChatSessionAsync();
    Task<string> ProcessChatRequestAsync(string modelName, ObservableCollection<MessageViewModel?> conversationMessages);
}