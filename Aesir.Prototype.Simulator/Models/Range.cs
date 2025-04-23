namespace Aesir.Prototype.Simulator.Models;

public class Range
{
    public double Min { get; set; }
    public double Max { get; set; }

    public Range(double min, double max)
    {
        Min = min;
        Max = max;
    }

    public bool Contains(double value)
    {
        return value >= Min && value <= Max;
    }
}