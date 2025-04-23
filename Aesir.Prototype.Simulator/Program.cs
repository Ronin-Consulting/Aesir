using Aesir.Prototype.Simulator.Utilities;

namespace Aesir.Prototype.Simulator;

public class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Aircraft Sensor Simulator");
        Console.WriteLine("========================");
        
        // Create and start simulator
        var simulator = new SensorSimulator();
        
        // Subscribe to sensor data events
        bool showAllSensors = false;
        simulator.SensorDataSent += (sender, packet) =>
        {
            if (showAllSensors)
            {
                Console.WriteLine($"\n--- SENSOR READINGS at {packet.Timestamp:HH:mm:ss.fff} ---");
                foreach (var reading in packet.Readings)
                {
                    string statusFlag = reading.Status != SensorStatus.Normal ? $" [{reading.Status}]" : "";
                    Console.WriteLine($"{reading.DisplayName}: {reading.ValueWithUnit}{statusFlag}");
                }
                Console.WriteLine("-------------------------------");
            }
        };
        
        simulator.Start();
        
        // Menu for user control
        bool running = true;
        while (running)
        {
            Console.WriteLine("\nCommands:");
            Console.WriteLine("1 - Trigger Left Engine (1) Temperature Warning");
            Console.WriteLine("2 - Trigger Left Engine (1) Temperature Critical");
            Console.WriteLine("3 - Trigger Right Engine (2) Temperature Critical");
            Console.WriteLine("4 - Trigger Right Engine (2) Oil Temperature Warning");
            Console.WriteLine("5 - Trigger Right Engine (2) Oil Temperature Critical");
            Console.WriteLine("6 - Trigger Low Fuel Pressure Warning");
            Console.WriteLine("7 - Trigger Low Fuel Pressure Critical");
            Console.WriteLine("8 - Trigger Low Fuel Quantity Warning");
            Console.WriteLine("9 - Trigger Electrical System Voltage Critical");
            Console.WriteLine("D - Toggle Display All Sensors (currently " + (showAllSensors ? "ON" : "OFF") + ")");
            Console.WriteLine("R - Reset All Sensors");
            Console.WriteLine("X - Exit");
            
            Console.Write("\nEnter command: ");
            var key = Console.ReadKey();
            Console.WriteLine();
            
            switch (key.KeyChar)
            {
                case '1':
                    simulator.TriggerSensorFailure("EngineTemp_Engine1", SensorStatus.Warning);
                    break;
                case '2':
                    simulator.TriggerSensorFailure("EngineTemp_Engine1", SensorStatus.Critical);
                    break;
                case '3':
                    simulator.TriggerSensorFailure("EngineTemp_Engine2", SensorStatus.Critical);
                    break;
                case '4':
                    simulator.TriggerSensorFailure("OilTemp_Engine2", SensorStatus.Warning);
                    break;
                case '5':
                    simulator.TriggerSensorFailure("OilTemp_Engine2", SensorStatus.Critical);
                    break;
                case '6':
                    simulator.TriggerSensorFailure("FuelPressure", SensorStatus.Warning);
                    break;
                case '7':
                    simulator.TriggerSensorFailure("FuelPressure", SensorStatus.Critical);
                    break;
                case '8':
                    simulator.TriggerSensorFailure("FuelQuantity", SensorStatus.Warning);
                    break;
                case '9':
                    simulator.TriggerSensorFailure("ElectricalVoltage", SensorStatus.Critical);
                    break;
                case 'd':
                case 'D':
                    showAllSensors = !showAllSensors;
                    Console.WriteLine($"Display all sensors: {(showAllSensors ? "ON" : "OFF")}");
                    break;
                case 'r':
                case 'R':
                    simulator.ResetAllSensors();
                    break;
                case 'x':
                case 'X':
                    running = false;
                    break;
                default:
                    Console.WriteLine("Invalid command.");
                    break;
            }
            
            await Task.Delay(100); // Small delay for UI responsiveness
        }
        
        // Stop simulator
        simulator.Stop();
        Console.WriteLine("Simulation stopped. Press any key to exit.");
        Console.ReadKey();
    }
}