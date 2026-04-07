using Godot;
using System.Collections.Generic;
using Terrabellum.Core;
using Terrabellum.Rendering;

namespace Terrabellum;

public partial class Main : Node
{
    public Tabletop Tabletop => _tabletop;
    private Tabletop _tabletop = new();
    private List<UnitView> _unitViews = new();
    private List<DieView> _diceViews = new();
    private InterfaceView? _interfaceView;

    public override void _Ready()
    {
        GD.Print("Terrabellum Initializing (3D)...");

        // Setup UI
        _interfaceView = new InterfaceView();
        AddChild(_interfaceView);

        // Setup 3D Environment
        SetupEnvironment();

        // Setup Table
        var tableView = new TableView();
        AddChild(tableView);

        // Setup Camera
        var camera = new CameraController();
        AddChild(camera);
        camera.Position = new Vector3(0, 500, 0);
        // Explicitly tell Godot that "Up" on screen should be World -Z
        camera.LookAt(Vector3.Zero, new Vector3(0, 0, -1));

        // Dice Setup
        for (int i = 0; i < 3; i++)
        {
            var die = new Die(6);
            var dieView = new DieView(die);
            dieView.Position = new Vector3(-300 + (i * 60), 0, 400);
            AddChild(dieView);
            _diceViews.Add(dieView);
        }

        // 1. Create dummy definitions
        var orcDefinition = new UnitDefinition
        {
            Id = "orc_blitzer",
            Name = "Orc Blitzer",
            Type = "Infantry",
            BaseShape = BaseShape.Circle,
            BaseSize = 32.0f,
            PointCost = 80
        };

        var marineDefinition = new UnitDefinition
        {
            Id = "space_marine",
            Name = "Space Marine",
            Type = "Infantry",
            BaseShape = BaseShape.Circle,
            BaseSize = 28.0f,
            PointCost = 20
        };

        var tankDefinition = new UnitDefinition
        {
            Id = "rhino_tank",
            Name = "Rhino",
            Type = "Vehicle",
            BaseShape = BaseShape.Square,
            BaseSize = 60.0f,
            PointCost = 100
        };

        // 2. Spawn units
        SpawnUnit(orcDefinition, new System.Numerics.Vector2(-100, -100), Colors.Green);
        SpawnUnit(marineDefinition, new System.Numerics.Vector2(100, -100), Colors.Blue);
        SpawnUnit(tankDefinition, new System.Numerics.Vector2(0, 100), Colors.Blue);
    }

    private void SetupEnvironment()
    {
        // Directional Light
        var light = new DirectionalLight3D();
        light.RotationDegrees = new Vector3(-45, 45, 0);
        light.ShadowEnabled = true;
        AddChild(light);

        // World Environment
        var env = new WorldEnvironment();
        var sky = new Sky();
        sky.SkyMaterial = new ProceduralSkyMaterial();
        
        var environment = new Godot.Environment();
        environment.BackgroundMode = Godot.Environment.BGMode.Sky;
        environment.Sky = sky;
        environment.AmbientLightSource = Godot.Environment.AmbientSource.Sky;
        
        env.Environment = environment;
        AddChild(env);
    }

    private void SpawnUnit(UnitDefinition def, System.Numerics.Vector2 position, Color playerColor)
    {
        var unit = new Unit(def)
        {
            Position = position
        };

        _tabletop.AddUnit(unit);

        // Create the view
        var view = new UnitView(unit)
        {
            PlayerColor = playerColor
        };
        
        AddChild(view);
        _unitViews.Add(view);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept") || (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Space))
        {
            foreach (var dieView in _diceViews)
            {
                dieView.StartRoll();
            }
        }
    }
}
