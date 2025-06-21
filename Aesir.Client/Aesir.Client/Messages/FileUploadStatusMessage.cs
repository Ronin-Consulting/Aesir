namespace Aesir.Client.Messages;

public class FileUploadStatusMessage
{
    public string? ConversationId { get; set; }
    public string FilePath { get; set; } = "No File";
    public bool IsProcessing { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}