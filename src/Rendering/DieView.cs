using Godot;
using Terrabellum.Core;

namespace Terrabellum.Rendering;

public partial class DieView : Node2D
{
    private readonly Die _die;
    private int _displayValue;
    private bool _isRolling;
    private double _rollTimer;
    private double _rollDuration = 0.6; // Time to "roll"
    private double _tickTimer;
    private double _tickInterval = 0.05; // Speed of value cycling

    public DieView(Die die)
    {
        _die = die;
        _displayValue = die.Sides; // Initial face
    }

    public void StartRoll()
    {
        _die.Roll(); // Determine result immediately
        _isRolling = true;
        _rollTimer = _rollDuration;
    }

    public override void _Process(double delta)
    {
        if (!_isRolling) return;

        _rollTimer -= delta;
        _tickTimer -= delta;

        if (_tickTimer <= 0)
        {
            _tickTimer = _tickInterval;
            // Cycle visually
            _displayValue = GD.RandRange(1, _die.Sides);
            QueueRedraw();
        }

        if (_rollTimer <= 0)
        {
            _isRolling = false;
            _displayValue = _die.LastResult;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        float size = 40.0f;
        float radius = size / 2.0f;
        
        // Draw Die Base
        Rect2 rect = new Rect2(-radius, -radius, size, size);
        DrawRect(rect, Colors.White);
        DrawRect(rect, Colors.Black, false, 2.0f);

        // Draw Value
        var font = ThemeDB.FallbackFont;
        DrawString(font, new Vector2(-radius, 5), _displayValue.ToString(), HorizontalAlignment.Center, size, 24, Colors.Black);
    }
}
