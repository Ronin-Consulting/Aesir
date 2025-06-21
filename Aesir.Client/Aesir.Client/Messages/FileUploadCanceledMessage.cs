namespace Aesir.Client.Messages;

public class FileUploadCanceledMessage
{
    public string? ConversationId { get; set; }
    public string FilePath { get; set; } = null!;
}