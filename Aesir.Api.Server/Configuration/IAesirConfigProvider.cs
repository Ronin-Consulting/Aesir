using Aesir.Api.Server.Models;

namespace Aesir.Api.Server.Configuration;

public interface IAesirConfigProvider
{
    List<AesirAgent> GetAgents();
    List<AesirMcpServer> GetMcpServers();
    List<AesirInferenceEngine> GetInferenceEngines();
    List<AesirTool> GetTools();
    AesirGeneralSettings GetGeneralSettings();
}
