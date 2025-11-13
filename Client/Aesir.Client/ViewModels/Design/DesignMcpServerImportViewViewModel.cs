using Aesir.Client.Services.Implementations.NoOp;
using CommunityToolkit.Mvvm.Input;

namespace Aesir.Client.ViewModels.Design;

public class DesignMcpServerImportViewViewModel : McpServerImportViewViewModel
{
    public DesignMcpServerImportViewViewModel() : base(new NoOpNotificationService(), new NoOpConfigurationService())
    {
        ClientConfigJson = "{\n  \"servers\": {\n    \"myCustomServer\": {\n      \"type\": \"sse\",\n      \"url\": \"http://localhost:5001/sse\",\n      \"headers\": {\n        \"Authorization\": \"Bearer YOUR_TOKEN\"\n      }\n    }\n  }\n}";
        
        OkCommand = new RelayCommand(() => { });
        CancelCommand = new RelayCommand(() => { });
    }
}