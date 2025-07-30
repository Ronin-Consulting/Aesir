using Aesir.Common.Models;

namespace Aesir.Client.Messages;

public class ShowAgentDetailMessage(AesirAgentBase? agent)
{   
    public AesirAgentBase Agent { get; set; } = agent ?? new AesirAgentBase();
}