using System;
using System.Collections.Generic;

namespace Terrabellum.Core;

public class Die
{
    private static readonly Random _rng = new();
    
    public int Sides { get; }
    public int LastResult { get; private set; }

    public Die(int sides = 6)
    {
        if (sides < 2) throw new ArgumentException("A die must have at least 2 sides.");
        Sides = sides;
    }

    public int Roll()
    {
        LastResult = _rng.Next(1, Sides + 1);
        return LastResult;
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
