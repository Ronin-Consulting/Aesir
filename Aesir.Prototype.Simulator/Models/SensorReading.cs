using Aesir.Prototype.Simulator.Utilities;

namespace Aesir.Prototype.Simulator.Models;

public class SensorReading
{
    public required string SensorId { get; set; }
    public SensorType Type { get; set; }
    public EnginePosition? Engine { get; set; }
    public double Value { get; set; }
    public Unit Unit { get; set; }
    public SensorStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
    public string DisplayName => Engine.HasValue ? $"{Type} (Engine {(int)Engine})" : Type.ToString();
    public string ValueWithUnit
    {
        get
        {
            string unitSymbol = Unit switch
            {
                Unit.DegreeCelsius => "°C",
                Unit.PSI => "psi",
                Unit.Percent => "%",
                Unit.Volt => "V",
                Unit.PPH => "pph",
                Unit.KTS => "kts",
                _ => ""
            };
            return $"{Value:F1} {unitSymbol}";
        }
    }
}