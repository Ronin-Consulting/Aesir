using CommunityToolkit.Mvvm.Collections;

namespace Aesir.Client.ViewModels.Interfaces;

public interface IChatHistoryViewModel
{
    ObservableGroupedCollection<string, ChatHistoryButtonViewModel> ChatHistoryByDate { get; }
    string SearchText { get; set; }
}