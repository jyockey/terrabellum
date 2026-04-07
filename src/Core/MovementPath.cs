using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Terrabellum.Core;

public class MovementPath
{
    public Unit Unit { get; }
    private List<Vector2> _waypoints = new();
    public IReadOnlyList<Vector2> Waypoints => _waypoints;

    public MovementPath(Unit unit)
    {
        Unit = unit;
        _waypoints.Add(unit.Position);
    }

    public void AddWaypoint(Vector2 point)
    {
        _waypoints.Add(point);
    }

    public float GetTotalDistance(Vector2 terminalPoint)
    {
        float distance = 0;
        for (int i = 0; i < _waypoints.Count - 1; i++)
        {
            distance += (_waypoints[i + 1] - _waypoints[i]).Length();
        }
        distance += (terminalPoint - _waypoints.Last()).Length();
        return distance;
    }

    public void Finalize(Vector2 finalPos)
    {
        Unit.Position = finalPos;
    }
}
