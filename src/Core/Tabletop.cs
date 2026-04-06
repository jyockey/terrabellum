using System.Collections.Generic;

namespace Terrabellum.Core;

public class Tabletop
{
    public List<Unit> Units { get; } = new();

    public void AddUnit(Unit unit)
    {
        Units.Add(unit);
    }

    public void RemoveUnit(Unit unit)
    {
        Units.Remove(unit);
    }
}
