using Aesir.Prototype.Simulator.Utilities;

namespace Aesir.Prototype.Simulator.Models;

public class SensorParameters
{
    public SensorType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public Unit Unit { get; set; }
    public double NormalMin { get; set; }
    public double NormalMax { get; set; }
    public double WarningMin { get; set; }
    public double WarningMax { get; set; }
    public double CriticalMin { get; set; }
    public double CriticalMax { get; set; }
}
