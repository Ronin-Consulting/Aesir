namespace Aesir.Client.Messages;

public class FileUploadRequestMessage
{
    public string? ConversationId { get; set; }
    public string FilePath { get; set; } = null!;
}