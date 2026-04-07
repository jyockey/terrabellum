using Godot;
using Terrabellum.Core;

namespace Terrabellum.Rendering;

public partial class UnitView : Node3D
{
	private Unit _unit;
	public Color PlayerColor { get; set; } = Colors.White;

	public Unit GetUnit() => _unit;

	private MeshInstance3D _baseMesh = new();
	private Label3D _label = new();
	private StaticBody3D _body = new();
	private CollisionShape3D _collisionShape = new();

	public UnitView(Unit unit)
	{
		_unit = unit;
		Position = new Vector3(_unit.Position.X, 0, _unit.Position.Y);
		Rotation = new Vector3(0, -_unit.Rotation, 0);
	}

	public override void _Ready()
	{
		_body.Name = "StaticBody3D";
		_collisionShape.Name = "CollisionShape3D";
		
		AddChild(_body);
		_body.AddChild(_baseMesh);
		_body.AddChild(_collisionShape);
		AddChild(_label);
		SetupVisuals();
	}

	private void SetupVisuals()
	{
		float size = _unit.Definition.BaseSize;
		float radius = size / 2.0f;

		var material = new StandardMaterial3D { AlbedoColor = PlayerColor };

		Shape3D godotShape;

		switch (_unit.Definition.BaseShape)
		{
			case BaseShape.Circle:
				var cylinder = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = 2.0f };
				_baseMesh.Mesh = cylinder;
				_baseMesh.Position = new Vector3(0, 1.0f, 0);
				godotShape = new CylinderShape3D { Radius = radius, Height = 2.0f };
				break;
			case BaseShape.Square:
				var box = new BoxMesh { Size = new Vector3(size, 2.0f, size) };
				_baseMesh.Mesh = box;
				_baseMesh.Position = new Vector3(0, 1.0f, 0);
				godotShape = new BoxShape3D { Size = new Vector3(size, 2.0f, size) };
				break;
			case BaseShape.Hex:
				var hex = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = 2.0f, RadialSegments = 6 };
				_baseMesh.Mesh = hex;
				_baseMesh.Position = new Vector3(0, 1.0f, 0);
				// We'll use a cylinder for hex collision as it's a close approximation
				godotShape = new CylinderShape3D { Radius = radius, Height = 2.0f };
				break;
			default:
				godotShape = new SphereShape3D { Radius = radius };
				break;
		}

		_baseMesh.SetSurfaceOverrideMaterial(0, material);
		_collisionShape.Shape = godotShape;
		_collisionShape.Position = new Vector3(0, 1.0f, 0);

		_label.Text = _unit.CustomName;
		_label.FontSize = 64;
		_label.PixelSize = 0.15f;
		_label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
		_label.Position = new Vector3(0, 10.0f + radius, 0);
	}

	public override void _Process(double delta)
	{
		Position = new Vector3(_unit.Position.X, 0, _unit.Position.Y);
		Rotation = new Vector3(0, -_unit.Rotation, 0);
	}
}
