namespace Aesir.Client.Messages;

public class SettingsHaveChangedMessage
{
    public SettingsType SettingsType { get; set; }
}

public enum SettingsType
{
    Other,
    InferenceEngines,
    General,
    McpServer,
    Tools,
    Agent
}