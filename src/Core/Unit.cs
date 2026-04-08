using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Terrabellum.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BaseShape
{
    Circle,
    Square,
    Hex
}

public class UnitDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int PointCost { get; set; }
    public BaseShape BaseShape { get; set; } = BaseShape.Circle;
    public float BaseSize { get; set; } = 32.0f; 
    public string ModelPath { get; set; } = string.Empty;
    public float ModelScale { get; set; } = 1.0f;
    public System.Numerics.Vector3 ModelOffset { get; set; } = System.Numerics.Vector3.Zero;
    public Dictionary<string, float> BaseStats { get; set; } = new();
    public List<string> Tags { get; set; } = new();

    public static UnitDefinition? LoadFromFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<UnitDefinition>(json, options);
        }
        catch (Exception e)
        {
            Godot.GD.PrintErr($"Failed to load UnitDefinition from {path}: {e.Message}");
            return null;
        }
    }
}

public class Unit
{
    public string Guid { get; private set; } = System.Guid.NewGuid().ToString();
    public UnitDefinition Definition { get; private set; }
    public string OwnerId { get; set; } = string.Empty;
    public string CustomName { get; set; } = string.Empty;
    
    // Position logic handled in "Tabletop" or "World" coordinates
    public System.Numerics.Vector2 Position { get; set; }
    public float Rotation { get; set; } 

    // Current state
    public Dictionary<string, float> CurrentStats { get; set; } = new();
    public List<string> StatusEffects { get; set; } = new();

    public Unit(UnitDefinition definition)
    {
        Definition = definition;
        CustomName = definition.Name;
        foreach (var stat in definition.BaseStats)
        {
            CurrentStats[stat.Key] = stat.Value;
        }
    }
}
