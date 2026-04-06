using Godot;
using Terrabellum.Core;
using System.Collections.Generic;

namespace Terrabellum.Rendering;

public partial class DieView : Node3D
{
    private readonly Die _die;
    private bool _isRolling;
    private double _rollTimer;
    private double _rollDuration = 0.6;
    private double _tickTimer;
    private double _tickInterval = 0.05;

    private MeshInstance3D _mesh = new();

    private static readonly Dictionary<int, Basis> FaceBases = new()
    {
        { 1, new Basis(new Vector3(1, 0, 0), new Vector3(0, -1, 0), new Vector3(0, 0, -1)) },
        { 2, new Basis(new Vector3(1, 0, 0), new Vector3(0, 0, -1), new Vector3(0, 1, 0)) },
        { 3, new Basis(new Vector3(0, 1, 0), new Vector3(0, 0, -1), new Vector3(-1, 0, 0)) },
        { 4, new Basis(new Vector3(0, -1, 0), new Vector3(0, 0, -1), new Vector3(1, 0, 0)) },
        { 5, new Basis(new Vector3(-1, 0, 0), new Vector3(0, 0, -1), new Vector3(0, -1, 0)) },
        { 6, Basis.Identity }
    };

    public DieView(Die die)
    {
        _die = die;
    }

    public override void _Ready()
    {
        AddChild(_mesh);
        var box = new BoxMesh { Size = new Vector3(40, 40, 40) };
        _mesh.Mesh = box;
        _mesh.Position = new Vector3(0, 20, 0);

        var material = new StandardMaterial3D { AlbedoColor = Colors.White };
        _mesh.SetSurfaceOverrideMaterial(0, material);

        SetupFaces();
        if (FaceBases.TryGetValue(_die.Sides, out Basis b)) _mesh.Basis = b;
    }

    private void SetupFaces()
    {
        AddFaceLabel(1, new Vector3(0, -20.1f, 0), new Vector3(Mathf.Pi/2, 0, 0));
        AddFaceLabel(2, new Vector3(0, 0, 20.1f), Vector3.Zero);
        AddFaceLabel(3, new Vector3(20.1f, 0, 0), new Vector3(0, Mathf.Pi/2, 0));
        AddFaceLabel(4, new Vector3(-20.1f, 0, 0), new Vector3(0, -Mathf.Pi/2, 0));
        AddFaceLabel(5, new Vector3(0, 0, -20.1f), new Vector3(0, Mathf.Pi, 0));
        AddFaceLabel(6, new Vector3(0, 20.1f, 0), new Vector3(-Mathf.Pi/2, 0, 0));
    }

    private void AddFaceLabel(int value, Vector3 position, Vector3 rotation)
    {
        var label = new Label3D();
        label.Text = value.ToString();
        label.FontSize = 128;
        label.PixelSize = 0.15f; 
        label.Modulate = Colors.Black;
        label.Position = position;
        label.Rotation = rotation;
        label.Billboard = BaseMaterial3D.BillboardModeEnum.Disabled;
        _mesh.AddChild(label);
    }

    public void StartRoll()
    {
        _die.Roll();
        _isRolling = true;
        _rollTimer = _rollDuration;
    }

    public override void _Process(double delta)
    {
        if (!_isRolling) return;
        _rollTimer -= delta;

        if (_rollTimer <= 0)
        {
            _isRolling = false;
            if (FaceBases.TryGetValue(_die.LastResult, out Basis targetBasis))
                _mesh.Basis = targetBasis;
            return;
        }

        _tickTimer -= delta;
        if (_tickTimer <= 0)
        {
            _tickTimer = _tickInterval;
            _mesh.RotationDegrees = new Vector3(GD.Randf() * 360, GD.Randf() * 360, GD.Randf() * 360);
        }
    }
}
