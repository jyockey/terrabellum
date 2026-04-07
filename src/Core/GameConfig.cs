using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terrabellum.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MovementStyle
{
    Free,
    Grid,
    Hex
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MeasurementUnit
{
    Inches,
    Centimeters,
    Pixels
}

public class DiceDefinition
{
    public string Name { get; set; } = string.Empty;
    public List<string> Faces { get; set; } = new();
}

public class GameConfig
{
    public string Name { get; set; } = string.Empty;
    public MovementStyle MovementStyle { get; set; } = MovementStyle.Free;
    public MeasurementUnit MeasurementUnit { get; set; } = MeasurementUnit.Inches;
    
    // How many world units represent one measurement unit (e.g., 100 units = 1 inch)
    public float UnitsPerMeasurement { get; set; } = 1.0f;

    public List<DiceDefinition> Dice { get; set; } = new();
    
    public string UnitSuffix => MeasurementUnit switch
    {
        MeasurementUnit.Inches => "\"",
        MeasurementUnit.Centimeters => "cm",
        _ => "px"
    };

    public static GameConfig? LoadFromFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<GameConfig>(json, options);
        }
        catch (System.Exception e)
        {
            Godot.GD.PrintErr($"Failed to load GameConfig from {path}: {e.Message}");
            return null;
        }
    }
}
