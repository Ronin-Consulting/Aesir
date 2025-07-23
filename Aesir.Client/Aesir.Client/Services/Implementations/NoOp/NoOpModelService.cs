using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aesir.Client.Services.Implementations.NoOp;

public class NoOpModelService : IModelService
{
    public async Task<IEnumerable<AesirModelInfo>> GetModelsAsync()
    {
        return await Task.FromResult(new List<AesirModelInfo>());
    }
}