using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Aesir.Prototype.Simulator.Models;
using Aesir.Prototype.Simulator.Utilities;

namespace Aesir.Prototype.Simulator;

public class SensorSimulator
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _ipEndPoint;
    private readonly Random _random = new Random();
    private CancellationTokenSource _cancellationTokenSource;

    private readonly Dictionary<SensorType, SensorParameters> _sensorParameters = new();
    
    private readonly Dictionary<string, double> _currentValues = new();
    private readonly Dictionary<string, SensorStatus> _currentStatuses = new();

    private readonly List<(string Id, SensorType Type, EnginePosition? Engine)> _sensors = [];
    
    // Event to notify when sensor data is sent
    public event EventHandler<SensorDataPacket> SensorDataSent;

    public SensorSimulator(int port = 11000)
    {
        _udpClient = new UdpClient();
        _ipEndPoint = new IPEndPoint(IPAddress.Loopback, port);
        
        InitializeSensorParameters();
        InitializeSensors();
    }

    public void Start()
    {
        _cancellationTokenSource = new CancellationTokenSource();

        Task.Run(async () =>
        {
            Console.WriteLine("Starting sensor simulation...");

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // This was missing in your original code
                await SimulateAndSendDataAsync(_cancellationTokenSource.Token);
                
                await Task.Delay(1000, _cancellationTokenSource.Token);
            }
        }, _cancellationTokenSource.Token);
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        _udpClient.Close(); 
        Console.WriteLine("Stopping sensor simulation...");
    }
    
    public void ResetAllSensors()
    {
        foreach (var (id, type, _) in _sensors)
        {
            var parameters = _sensorParameters[type];
            var normalValue = _random.NextDouble() * 
                (parameters.NormalMax - parameters.NormalMin) + parameters.NormalMin;
                
            _currentValues[id] = normalValue;
            _currentStatuses[id] = SensorStatus.Normal;
        }
            
        Console.WriteLine("All sensors reset to normal operation");
    }
    
    public void TriggerSensorFailure(string sensorId, SensorStatus severity)
    {
        if (!_currentValues.ContainsKey(sensorId))
        {
            Console.WriteLine($"Sensor not found: {sensorId}");
            return;
        }
        
        var sensorInfo = _sensors.Find(s => s.Id == sensorId);
        var parameters = _sensorParameters[sensorInfo.Type];
        
        // Set anomalous value
        double anomalyValue;
        
        if (severity == SensorStatus.Warning)
        {
            // Set to warning range
            if (_random.Next(2) == 0)
            {
                // Low warning
                anomalyValue = _random.NextDouble() * 
                    (parameters.NormalMin - parameters.WarningMin) + parameters.WarningMin;
            }
            else
            {
                // High warning
                anomalyValue = _random.NextDouble() * 
                    (parameters.WarningMax - parameters.NormalMax) + parameters.NormalMax;
            }
        }
        else // Critical
        {
            // Set to critical range
            if (_random.Next(2) == 0 && parameters.CriticalMin > 0)
            {
                // Low critical
                anomalyValue = _random.NextDouble() * 
                    (parameters.WarningMin - parameters.CriticalMin) + parameters.CriticalMin;
            }
            else
            {
                // High critical
                anomalyValue = _random.NextDouble() * 
                    (parameters.CriticalMax - parameters.WarningMax) + parameters.WarningMax;
            }
        }
        
        _currentValues[sensorId] = anomalyValue;
        _currentStatuses[sensorId] = severity;
        
        var unitSymbol = parameters.Unit switch
        {
            Unit.DegreeCelsius => "°C",
            Unit.PSI => "psi",
            Unit.Percent => "%",
            Unit.Volt => "V",
            Unit.PPH => "pph",
            Unit.KTS => "kts",
            _ => ""
        };
        
        Console.WriteLine($"Triggered {severity} for {sensorId}: " +
                          $"Value set to {anomalyValue:F1} {unitSymbol}");
    }
    
    private async Task SimulateAndSendDataAsync(CancellationToken token)
    {
        var readings = new List<SensorReading>();
        var timestamp = DateTime.Now;
            
        // Update and collect readings for all sensors
        foreach (var (id, type, engine) in _sensors)
        {
            var reading = SimulateSensorReading(id, type, engine, timestamp);
            readings.Add(reading);
        }
            
        // Create and send the packet
        var packet = new SensorDataPacket
        {
            Timestamp = timestamp,
            Readings = readings
        };
            
        await SendPacketAsync(packet, token);
        
        // Fire the event with the packet data
        SensorDataSent?.Invoke(this, packet);
            
        // Log any anomalous readings
        LogAnomalousReadings(readings);
    }

    private SensorReading SimulateSensorReading(string id, SensorType type, EnginePosition? engine, DateTime timestamp)
    {
        double currentValue = _currentValues[id];
        SensorStatus currentStatus = _currentStatuses[id];
        var parameters = _sensorParameters[type];

        if (currentStatus == SensorStatus.Normal)
        {
            // Small random fluctuations (but never becoming anomalous on their own)
            double delta = ((_random.NextDouble() * 2) - 1.0) * (parameters.NormalMax - parameters.NormalMin) * 0.01;
            double newValue = currentValue + delta;
            
            // Keep in normal range
            newValue = Math.Max(parameters.NormalMin, Math.Min(parameters.NormalMax, newValue));
            
            // Update stored values
            _currentValues[id] = newValue;
            currentValue = newValue;
        }
        
        var reading = new SensorReading
        {
            SensorId = id,
            Type = type,
            Engine = engine,
            Value = Math.Round(currentValue, 1),
            Unit = parameters.Unit,
            Status = currentStatus,
            Timestamp = timestamp,
        };

        return reading;
    }
    
    private async Task SendPacketAsync(SensorDataPacket packet, CancellationToken token)
    {
        try
        {
            // Serialize to JSON
            var json = JsonSerializer.Serialize(packet, new JsonSerializerOptions
            {
                WriteIndented = false
            });
                
            // Send via UDP
            var bytes = Encoding.UTF8.GetBytes(json);
            await _udpClient.SendAsync(bytes, _ipEndPoint, token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending data: {ex.Message}");
        }
    }
    
    private void LogAnomalousReadings(List<SensorReading> readings)
    {
        var anomalies = readings.FindAll(r => r.Status != SensorStatus.Normal);
            
        if (anomalies.Count > 0)
        {
            Console.WriteLine($"--- ANOMALOUS READINGS ({anomalies.Count}) ---");
                
            foreach (var reading in anomalies)
            {
                Console.WriteLine($"{reading.DisplayName}: {reading.ValueWithUnit} [{reading.Status}]");
            }
        }
    }
    
    private void InitializeSensorParameters()
    {
        // Engine Temperature parameters
        _sensorParameters[SensorType.EngineTemperature] = new SensorParameters
        {
            Type = SensorType.EngineTemperature,
            Name = "Engine Temperature",
            Unit = Unit.DegreeCelsius,
            NormalMin = 300,
            NormalMax = 850,
            WarningMin = 200,
            WarningMax = 900,
            CriticalMin = 0,
            CriticalMax = 950,
        };
        
        // Oil Temperature parameters
        _sensorParameters[SensorType.OilTemperature] = new SensorParameters
        {
            Type = SensorType.OilTemperature,
            Name = "Oil Temperature",
            Unit = Unit.DegreeCelsius,
            NormalMin = 40,
            NormalMax = 90,
            WarningMin = 20,
            WarningMax = 110,
            CriticalMin = 0,
            CriticalMax = 120,
        };
        
        // Fuel Pressure parameters
        _sensorParameters[SensorType.FuelPressure] = new SensorParameters
        {
            Type = SensorType.FuelPressure,
            Name = "Fuel Pressure",
            Unit = Unit.PSI,
            NormalMin = 30,
            NormalMax = 55,
            WarningMin = 15,
            WarningMax = 65,
            CriticalMin = 0,
            CriticalMax = 10,
        };
        
        // Fuel Quantity parameters
        _sensorParameters[SensorType.FuelQuantity] = new SensorParameters
        {
            Type = SensorType.FuelQuantity,
            Name = "Fuel Quantity",
            Unit = Unit.Percent,
            NormalMin = 15,
            NormalMax = 100,
            WarningMin = 5,
            WarningMax = 100,
            CriticalMin = 0,
            CriticalMax = 5,
        };
        
        // Electrical Voltage parameters
        _sensorParameters[SensorType.ElectricalVoltage] = new SensorParameters
        {
            Type = SensorType.ElectricalVoltage,
            Name = "Electrical System Voltage",
            Unit = Unit.Volt,
            NormalMin = 27,
            NormalMax = 29,
            WarningMin = 24,
            WarningMax = 31,
            CriticalMin = 0,
            CriticalMax = 24,
        };
        
        // Engine Vibration parameters
        _sensorParameters[SensorType.EngineVibration] = new SensorParameters
        {
            Type = SensorType.EngineVibration,
            Name = "Engine Vibration",
            Unit = Unit.KTS,
            NormalMin = 0,
            NormalMax = 2,
            WarningMin = 0,
            WarningMax = 4,
            CriticalMin = 0,
            CriticalMax = 5,
        };
    }

    private void InitializeSensors()
    {
        foreach(var enginePos in Enum.GetValues<EnginePosition>())
        {
            _sensors.Add(($"EngineTemp_Engine{(int)enginePos}", SensorType.EngineTemperature, enginePos));
                
            // Oil temperature sensors (one per engine)
            _sensors.Add(($"OilTemp_Engine{(int)enginePos}", SensorType.OilTemperature, enginePos));
                
            // Engine vibration sensors (one per engine)
            _sensors.Add(($"Vibration_Engine{(int)enginePos}", SensorType.EngineVibration, enginePos));
        }
        
        _sensors.Add(("FuelPressure", SensorType.FuelPressure, null));
        _sensors.Add(("FuelQuantity", SensorType.FuelQuantity, null));
        _sensors.Add(("ElectricalVoltage", SensorType.ElectricalVoltage, null));
        
        foreach (var (id, type, _) in _sensors)
        {
            var parameters = _sensorParameters[type];
            var normalValue = _random.NextDouble() * 
                (parameters.NormalMax - parameters.NormalMin) + parameters.NormalMin;
                
            _currentValues[id] = normalValue;
            _currentStatuses[id] = SensorStatus.Normal;
        }
    }
}