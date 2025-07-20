using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aesir.Client.Models;
using Aesir.Common.Models;

namespace Aesir.Client.Services.Implementations.NoOp;

public class NoOpModelService : IModelService
{
    public async Task<IEnumerable<AesirModelInfo>> GetModelsAsync()
    {
        return await Task.FromResult(new List<AesirModelInfo>());
    }
}