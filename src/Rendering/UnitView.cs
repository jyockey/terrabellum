using Godot;
using Terrabellum.Core;

namespace Terrabellum.Rendering;

public partial class UnitView : Node3D
{
	private Unit _unit;
	public Color PlayerColor { get; set; } = Colors.White;

	public Unit GetUnit() => _unit;

	private MeshInstance3D _baseMesh = new();
	private MeshInstance3D _facingIndicator = new();
	private Node3D? _modelNode;
	private StaticBody3D _body = new();
	private CollisionShape3D _collisionShape = new();

	public UnitView(Unit unit)
	{
		_unit = unit;
		Position = RenderScale.ToWorldPos(_unit.Position);
		Rotation = new Vector3(0, -_unit.Rotation, 0);
	}

	public override void _Ready()
	{
		_body.Name = "StaticBody3D";
		_collisionShape.Name = "CollisionShape3D";
		
		AddChild(_body);
		_body.AddChild(_baseMesh);
		_body.AddChild(_facingIndicator);
		_body.AddChild(_collisionShape);

		if (_unit.Definition.Model != null && !string.IsNullOrEmpty(_unit.Definition.Model.Path))
		{
			LoadModel(_unit.Definition.Model);
		}

		SetupVisuals();
		SetupFacingIndicator();
	}

	private void SetupFacingIndicator()
	{
		float worldRadius = RenderScale.ToWorld(_unit.Definition.BaseSize / 2.0f);
		
		var headSize = RenderScale.ToWorld(RenderScale.IndicatorSize);
		var shaftHeight = worldRadius * 0.6f; 
		var shaftWidth = headSize * 0.4f; // Derived from head size
		var shaftThickness = RenderScale.ToWorld(RenderScale.BaseHeight / 4.0f);
		
		var shaft = new BoxMesh { Size = new Vector3(shaftWidth, shaftThickness, shaftHeight) };
		_facingIndicator.Mesh = shaft;
		
		// Position slightly above the base height
		float yOffset = RenderScale.WorldBaseHeight + RenderScale.ToWorld(RenderScale.BaseHeight / 10.0f);
		_facingIndicator.Position = new Vector3(0, yOffset, -shaftHeight / 2.0f - worldRadius * 0.1f);
		
		var material = new StandardMaterial3D { AlbedoColor = Colors.Black, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
		_facingIndicator.SetSurfaceOverrideMaterial(0, material);
		
		var arrowhead = new MeshInstance3D();
		var headMesh = new CylinderMesh { TopRadius = 0, BottomRadius = headSize / 2.0f, Height = headSize, RadialSegments = 3 };
		arrowhead.Mesh = headMesh;
		arrowhead.RotationDegrees = new Vector3(-90, 0, 0); 
		arrowhead.Position = new Vector3(0, 0, -shaftHeight / 2.0f - headSize / 2.0f);
		arrowhead.SetSurfaceOverrideMaterial(0, material);
		_facingIndicator.AddChild(arrowhead);
	}

	private void LoadModel(ModelDefinition model)
	{
		try
		{
			var scene = GD.Load<PackedScene>(model.Path);
			if (scene != null)
			{
				_modelNode = scene.Instantiate<Node3D>();
				_body.AddChild(_modelNode);
				
				// Apply base height + logical offsets
				var offset = model.Offset;
				_modelNode.Position = RenderScale.ToWorldPos(new System.Numerics.Vector2(offset.X, offset.Z), RenderScale.WorldBaseHeight + RenderScale.ToWorld(offset.Y)); 
				_modelNode.RotationDegrees = new Vector3(0, model.Rotation, 0);

				// Imported models match the world scale multiplied by definition scale
				_modelNode.Scale = RenderScale.ModelScale * model.Scale;

				ApplyModelMaterial(_modelNode);
			}
		}
		catch (System.Exception e)
		{
			GD.PrintErr($"Failed to load model at {model.Path}: {e.Message}");
		}
	}

	private void ApplyModelMaterial(Node node)
	{
		// Create a subtle "color wash" by starting with a light grey base and adding 15% of the player color
		var washColor = new Color(0.7f, 0.7f, 0.7f).Lerp(PlayerColor, 0.15f);
		var material = new StandardMaterial3D 
		{ 
			AlbedoColor = washColor,
			Roughness = 0.8f, 
			VertexColorUseAsAlbedo = false 
		};

		foreach (var child in node.GetChildren())
		{
			if (child is MeshInstance3D mesh)
			{
				mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
				for (int i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
				{
					mesh.SetSurfaceOverrideMaterial(i, material);
				}
			}
			else if (child is Node childNode)
				ApplyModelMaterial(childNode);
		}
	}

	private void SetupVisuals()
	{
		float worldSize = RenderScale.ToWorld(_unit.Definition.BaseSize);
		float worldRadius = worldSize / 2.0f;
		float baseHeight = RenderScale.WorldBaseHeight;

		var material = new StandardMaterial3D { AlbedoColor = PlayerColor, Transparency = BaseMaterial3D.TransparencyEnum.Alpha };
		material.AlbedoColor = new Color(PlayerColor.R, PlayerColor.G, PlayerColor.B, 0.6f);

		Shape3D godotShape;
		switch (_unit.Definition.BaseShape)
		{
			case BaseShape.Circle:
				var cylinder = new CylinderMesh { TopRadius = worldRadius, BottomRadius = worldRadius, Height = baseHeight };
				_baseMesh.Mesh = cylinder;
				godotShape = new CylinderShape3D { Radius = worldRadius, Height = baseHeight };
				break;
			case BaseShape.Square:
				var box = new BoxMesh { Size = new Vector3(worldSize, baseHeight, worldSize) };
				_baseMesh.Mesh = box;
				godotShape = new BoxShape3D { Size = new Vector3(worldSize, baseHeight, worldSize) };
				break;
			case BaseShape.Hex:
				var hex = new CylinderMesh { TopRadius = worldRadius, BottomRadius = worldRadius, Height = baseHeight, RadialSegments = 6 };
				_baseMesh.Mesh = hex;
				godotShape = new CylinderShape3D { Radius = worldRadius, Height = baseHeight };
				break;
			default:
				godotShape = new SphereShape3D { Radius = worldRadius };
				break;
		}

		_baseMesh.Position = new Vector3(0, baseHeight / 2.0f, 0);
		_baseMesh.SetSurfaceOverrideMaterial(0, material);
		_collisionShape.Shape = godotShape;
		_collisionShape.Position = new Vector3(0, baseHeight / 2.0f, 0);
	}

	public override void _Process(double delta)
	{
		Position = RenderScale.ToWorldPos(_unit.Position);
		Rotation = new Vector3(0, -_unit.Rotation, 0);
	}
}
