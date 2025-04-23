namespace Aesir.Prototype.Simulator.Models;

public class SensorDataPacket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public List<SensorReading> Readings { get; set; } = [];
}