using Godot;
using Terrabellum.Core;

namespace Terrabellum.Rendering;

public partial class UnitView : Node2D
{
	private Unit _unit;
	public Color PlayerColor { get; set; } = Colors.White;

	public UnitView(Unit unit)
	{
		_unit = unit;
		// Sync initial position
		Position = new Vector2(_unit.Position.X, _unit.Position.Y);
		Rotation = _unit.Rotation;
	}

	public override void _Process(double delta)
	{
		// Simple one-way sync from logic to view for now
		Position = new Vector2(_unit.Position.X, _unit.Position.Y);
		Rotation = _unit.Rotation;
	}

	public override void _Draw()
	{
		float size = _unit.Definition.BaseSize;
		float radius = size / 2.0f;

		switch (_unit.Definition.BaseShape)
		{
			case BaseShape.Circle:
				DrawCircle(Vector2.Zero, radius, PlayerColor);
				DrawArc(Vector2.Zero, radius, 0, Mathf.Pi * 2, 64, Colors.Black, 2.0f);
				break;
			case BaseShape.Square:
				Rect2 rect = new Rect2(-radius, -radius, size, size);
				DrawRect(rect, PlayerColor);
				DrawRect(rect, Colors.Black, false, 2.0f);
				break;
			case BaseShape.Hex:
				DrawHex(radius, PlayerColor);
				break;
		}

		// Facing Indicator
		DrawLine(Vector2.Zero, Vector2.Right * radius, Colors.Black, 2.0f);

		// Label
		var font = ThemeDB.FallbackFont;
		DrawString(font, new Vector2(-radius, -radius - 10), _unit.CustomName, HorizontalAlignment.Center, size, 14, Colors.White);
	}

	private void DrawHex(float radius, Color color)
	{
		Vector2[] points = new Vector2[6];
		for (int i = 0; i < 6; i++)
		{
			float angle = Mathf.DegToRad(i * 60);
			points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
		}

		DrawColoredPolygon(points, color);
		for (int i = 0; i < 6; i++)
		{
			DrawLine(points[i], points[(i + 1) % 6], Colors.Black, 2.0f);
		}
	}
}
