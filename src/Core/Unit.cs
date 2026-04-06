using System;
using System.Collections.Generic;

namespace Terrabellum.Core;

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
    public Dictionary<string, float> BaseStats { get; set; } = new();
    public List<string> Tags { get; set; } = new();
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
