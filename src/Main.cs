using Godot;
using System.Collections.Generic;
using System.Linq;
using Terrabellum.Core;
using Terrabellum.Rendering;

namespace Terrabellum;

public partial class Main : Node
{
    public Tabletop Tabletop => _tabletop;
    public GameConfig Config { get; private set; } = new();

    private Tabletop _tabletop = new();
    private Dictionary<Unit, UnitView> _unitViews = new();
    private List<DieView> _diceViews = new();
    private InterfaceView? _interfaceView;

    public UnitView? GetUnitView(Unit unit) => _unitViews.GetValueOrDefault(unit);

    public override void _Ready()
    {
        GD.Print("Terrabellum Initializing (3D)...");

        // Load Game Config
        string configPath = ProjectSettings.GlobalizePath("res://config/games/warcrow.json");
        Config = GameConfig.LoadFromFile(configPath) ?? new GameConfig { Name = "Default" };
        GD.Print($"Loaded Game Config: {Config.Name} ({Config.MeasurementUnit})");

        // Setup UI
        _interfaceView = new InterfaceView(Config);
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
        camera.LookAt(Vector3.Zero, new Vector3(0, 0, -1));

        SetupDice();

        // 2. Spawn units from config
        var orcDefinition = LoadUnit("centaur");
        var marineDefinition = LoadUnit("space_marine");
        var tankDefinition = LoadUnit("rhino_tank");

        if (orcDefinition != null) SpawnUnit(orcDefinition, new System.Numerics.Vector2(-100, -100), Colors.Green);
        if (marineDefinition != null) SpawnUnit(marineDefinition, new System.Numerics.Vector2(100, -100), Colors.Blue);
        if (tankDefinition != null) SpawnUnit(tankDefinition, new System.Numerics.Vector2(0, 100), Colors.Blue);
    }

    private UnitDefinition? LoadUnit(string name)
    {
        string path = ProjectSettings.GlobalizePath($"res://config/units/{name}.json");
        return UnitDefinition.LoadFromFile(path);
    }

    private void SetupEnvironment()
    {
        // Directional Light
        var light = new DirectionalLight3D();
        light.RotationDegrees = new Vector3(-45, 45, 0);
        light.ShadowEnabled = true;
        light.LightEnergy = 1.2f; // Increased for better contrast
        light.ShadowBias = 0.002f; // Much smaller for mm scale (2 microns)
        light.ShadowNormalBias = 0.01f; 
        light.ShadowBlur = 0.5f;
        AddChild(light);

        // World Environment
        var env = new WorldEnvironment();
        var sky = new Sky();
        sky.SkyMaterial = new ProceduralSkyMaterial();
        
        var environment = new Godot.Environment();
        environment.BackgroundMode = Godot.Environment.BGMode.Sky;
        environment.Sky = sky;
        environment.AmbientLightSource = Godot.Environment.AmbientSource.Sky;
        environment.AmbientLightEnergy = 0.1f; // Lower ambient for deeper shadows
        
        // Enable High-Detail features
        environment.SsaoEnabled = true;
        environment.SsaoIntensity = 2.0f;
        environment.SsaoRadius = 5.0f; // 5mm radius for AO
        
        environment.SsilEnabled = true;
        
        environment.TonemapMode = Godot.Environment.ToneMapper.Filmic;
        
        env.Environment = environment;
        AddChild(env);
    }

    private void SetupDice()
    {
        float startX = -(Config.Dice.Count - 1) * 60; 
        for (int i = 0; i < Config.Dice.Count; i++)
        {
            var def = Config.Dice[i];
            var die = new Die(def.Name, def.Faces.ToArray());
            var dieView = new DieView(die);
            dieView.Position = new Vector3(startX + (i * 120), 10, 400);
            AddChild(dieView);
            _diceViews.Add(dieView);
        }
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
        _unitViews[unit] = view;
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
