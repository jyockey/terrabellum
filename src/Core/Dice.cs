using System;
using System.Collections.Generic;

namespace Terrabellum.Core;

public class Die
{
    private static readonly Random _rng = new();
    
    public string Name { get; }
    public string[] Faces { get; }
    public int Sides => Faces.Length;
    public int LastResultIndex { get; private set; }
    public string LastResultValue => Faces[LastResultIndex];

    public Die(string name, string[] faces)
    {
        if (faces.Length < 2) throw new ArgumentException("A die must have at least 2 sides.");
        Name = name;
        Faces = faces;
    }

    public Die(int sides = 6)
    {
        if (sides < 2) throw new ArgumentException("A die must have at least 2 sides.");
        Name = $"d{sides}";
        Faces = new string[sides];
        for (int i = 0; i < sides; i++)
        {
            string val = (i + 1).ToString();
            if (sides > 6 && (val == "6" || val == "9")) val += ".";
            Faces[i] = val;
        }
    }

    public int Roll()
    {
        LastResultIndex = _rng.Next(0, Sides);
        return LastResultIndex + 1; // Maintain 1-based return for legacy if needed
    }
}

public class DicePool
{
    public List<Die> Dice { get; } = new();
    public List<int> LastResults { get; } = new();

    public void AddDice(int count, int sides = 6)
    {
        for (int i = 0; i < count; i++)
        {
            Dice.Add(new Die(sides));
        }
    }

    public List<int> RollAll()
    {
        LastResults.Clear();
        foreach (var die in Dice)
        {
            LastResults.Add(die.Roll());
        }
        return LastResults;
    }
}
