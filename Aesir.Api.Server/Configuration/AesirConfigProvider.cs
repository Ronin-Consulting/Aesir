using Aesir.Api.Server.Models;
using Aesir.Api.Server.Services;

namespace Aesir.Api.Server.Configuration;

public class AesirConfigProvider : IAesirConfigProvider
{
    private readonly IConfiguration _configuration;
    private readonly IConfigurationService _configurationService;
    private readonly bool _loadFromDatabase;
    private readonly Lazy<AesirGeneralSettings> _generalSettings;
    private readonly Lazy<List<AesirInferenceEngine>> _inferenceEngines;
    private readonly Lazy<List<AesirAgent>> _agents;
    private readonly Lazy<List<AesirTool>> _tools;
    private readonly Lazy<List<AesirMcpServer>> _mcpServers;
    
    public AesirConfigProvider(IConfiguration  configuration,IConfigurationService configurationService)
    {
        _configuration = configuration;
        _configurationService=configurationService;
        _loadFromDatabase = _configuration.GetValue<bool>("Configuration:LoadFromDatabase", false);
        
        _generalSettings=new Lazy<AesirGeneralSettings>(()  =>_configurationService.GetGeneralSettingsAsync().GetAwaiter().GetResult() );
        _inferenceEngines = new Lazy<List<AesirInferenceEngine>>(()=>_configurationService.GetInferenceEnginesAsync().GetAwaiter().GetResult().ToList());
        _agents = new Lazy<List<AesirAgent>>(()=>_configurationService.GetAgentsAsync().GetAwaiter().GetResult().ToList());
        _tools=new Lazy<List<AesirTool>>(()=>_configurationService.GetToolsAsync().GetAwaiter().GetResult().ToList());
        _mcpServers=new Lazy<List<AesirMcpServer>>(()=>_configurationService.GetMcpServersAsync().GetAwaiter().GetResult().ToList());
    }

    public AesirGeneralSettings GetGeneralSettings()
    {
        return _loadFromDatabase ? _generalSettings.Value : _configuration.GetSection("GeneralSettings")
            .Get<AesirGeneralSettings>() ?? new AesirGeneralSettings();
    }

    public List<AesirInferenceEngine> GetInferenceEngines()
    {
        return _loadFromDatabase ? _inferenceEngines.Value : _configuration.GetSection("InferenceEngines")
            .Get<List<AesirInferenceEngine>>() ?? new List<AesirInferenceEngine>(); 
    }
    
    public List<AesirAgent> GetAgents()
    {
        return _loadFromDatabase ? _agents.Value : _configuration.GetSection("Agents").Get<List<AesirAgent>>() ??  new List<AesirAgent>();
    }

    public List<AesirTool> GetTools()
    {
        return _loadFromDatabase ? _tools.Value :  _configuration.GetSection("Tools").Get<List<AesirTool>>() ??  new List<AesirTool>();
    }
    
    public List<AesirMcpServer> GetMcpServers()
    {
        return _loadFromDatabase ? _mcpServers.Value : _configuration.GetSection("McpServers").Get<List<AesirMcpServer>>() ?? new List<AesirMcpServer>();
    }

}
