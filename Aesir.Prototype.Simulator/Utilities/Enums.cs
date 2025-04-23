namespace Aesir.Prototype.Simulator.Utilities;

// Represents a category of aircraft system
    public enum SystemCategory
    {
        Engine,
        Fuel,
        Hydraulic,
        Electrical,
        Environmental,
        Flight
    }
    
    // Engine identifier (C-130 has 4 engines)
    public enum EnginePosition
    {
        One = 1,    // Outboard left
        Two = 2,    // Inboard left
        Three = 3,  // Inboard right
        Four = 4    // Outboard right
    }
    
    // Represents measurement units
    public enum Unit
    {
        DegreeCelsius,  // °C - For temperatures
        PSI,            // Pounds per square inch - For pressures
        Percent,        // % - For quantities
        Volt,           // V - For electrical readings
        PPH,            // Pounds per hour - For fuel flow
        KTS             // Knots - For vibration/shaking
    }
    
    // Specific sensor types
    public enum SensorType
    {
        EngineTemperature,  // For engine fires/overheating
        OilTemperature,     // For high oil temperature 
        FuelPressure,       // For fuel system failure 
        FuelQuantity,       // For fuel system failure/fuel dumping 
        ElectricalVoltage,  // For electrical systems failure 
        EngineVibration     // For engine failure/propeller malfunctions
    }
    
    // Represents the health status of a sensor reading
    public enum SensorStatus
    {
        Normal,     // Operating within normal parameters
        Warning,    // Outside normal range but not critical
        Critical    // Emergency condition requiring checklist procedure
    }