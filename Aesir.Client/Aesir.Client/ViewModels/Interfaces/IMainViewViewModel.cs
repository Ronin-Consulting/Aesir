using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Aesir.Client.ViewModels.Interfaces;

public interface IMainViewViewModel
{
    bool MicOn { get; set; }
    bool PanelOpen { get; set; }
    bool SendingChatOrProcessingFile { get; set; }
    bool HasChatMessage { get; set; }
    bool ConversationStarted { get; set; }
    string? SelectedModelName { get; set; }
    string? ChatMessage { get; set; }
    string? ErrorMessage { get; set; }
    
    ObservableCollection<MessageViewModel?> ConversationMessages { get; }
    
    ICommand ToggleChatHistory { get; }
    ICommand ToggleNewChat { get; }
    ICommand ToggleMicrophone { get; }
    
    Task SendMessageAsync();
    Task ShowFileSelectionAsync();
}