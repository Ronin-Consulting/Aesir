using CommunityToolkit.Mvvm.Collections;

namespace Aesir.Client.ViewModels.Interfaces;

/// Represents the view model interface for displaying and managing chat history.
/// This interface is designed to facilitate binding chat history data to the UI
/// and provides members for integrating grouping and search functionality.
public interface IChatHistoryViewModel
{
    /// Represents a grouped collection of chat history items organized by date.
    /// This property retrieves the chat history categorized into groups,
    /// where each group corresponds to a specific date.
    /// The keys of the groups represent the dates, and the associated values
    /// are collections of `ChatHistoryButtonViewModel` instances.
    /// This structure facilitates grouping and displaying chat session
    /// history in a user-friendly and organized manner.
    ObservableGroupedCollection<string, ChatHistoryButtonViewModel> ChatHistoryByDate { get; }

    /// Gets or sets the search text value used for filtering or querying within the chat history view.
    /// This property represents the current text input that determines the criteria for searching or
    /// filtering chat history data displayed in the user interface.
    string SearchText { get; set; }
}