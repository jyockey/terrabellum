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
    public GameState State { get; private set; } = new();

    private Tabletop _tabletop = new();
    private Dictionary<Unit, UnitView> _unitViews = new();
    private List<DieView> _diceViews = new();
    private InterfaceView? _interfaceView;
    private CameraController? _camera;
    private TabletopView? _tabletopInput;

    public UnitView? GetUnitView(Unit unit) => _unitViews.GetValueOrDefault(unit);

    public override void _Ready()
    {
        GD.Print("Terrabellum Initializing (Meters Scale)...");

        // Load Game Config
        string configPath = ProjectSettings.GlobalizePath("res://config/games/warcrow.json");
        Config = GameConfig.LoadFromFile(configPath) ?? new GameConfig { Name = "Default" };
        GD.Print($"Loaded Game Config: {Config.Name} ({Config.MeasurementUnit})");

        // Setup Camera
        _camera = new CameraController();
        _camera.Position = new Vector3(0, RenderScale.ToWorld(600f), RenderScale.ToWorld(600f)); // 600mm up/back
        _camera.LookAt(Vector3.Zero, Vector3.Up);
        AddChild(_camera);

        // Setup Input Hierachy (Tabletop View as base)
        var inputLayer = new CanvasLayer { Layer = 0 };
        _tabletopInput = new TabletopView();
        _tabletopInput.Setup(_camera);
        inputLayer.AddChild(_tabletopInput);
        AddChild(inputLayer);

        // Setup UI (Drawn above everything)
        _interfaceView = new InterfaceView(Config, State);
        _interfaceView.Layer = 1;
        AddChild(_interfaceView);

        // Setup 3D Environment
        SetupEnvironment();

        // Setup Table
        var tableView = new TableView();
        AddChild(tableView);

        // 2. Spawn units from config
        var orcDefinition = LoadUnit("centaur");
        var marineDefinition = LoadUnit("space_marine");
        var tankDefinition = LoadUnit("rhino_tank");
        var duckDefinition = LoadUnit("duck");

        // Spawn positions use logical coordinates (millimeters)
        if (orcDefinition != null) SpawnUnit(orcDefinition, new System.Numerics.Vector2(-100f, -100f), Colors.Green);
        if (marineDefinition != null) SpawnUnit(marineDefinition, new System.Numerics.Vector2(100f, -100f), Colors.Blue);
        if (tankDefinition != null) SpawnUnit(tankDefinition, new System.Numerics.Vector2(0, 100f), Colors.Blue);
        if (duckDefinition != null) SpawnUnit(duckDefinition, new System.Numerics.Vector2(0, -100f), Colors.Yellow);
    }

    private UnitDefinition? LoadUnit(string name)
    {
        string path = ProjectSettings.GlobalizePath($"res://config/units/{name}.json");
        return UnitDefinition.LoadFromFile(path);
    }

    private void SetupEnvironment()
    {
        // Primary Key Light (Main Sun)
        var keyLight = new DirectionalLight3D();
        keyLight.Name = "KeyLight";
        keyLight.RotationDegrees = new Vector3(-60, 45, 0); 
        keyLight.ShadowEnabled = true;
        keyLight.LightEnergy = 1.2f; 
        
        // Godot standard settings for meters scale
        keyLight.DirectionalShadowMaxDistance = 5.0f; // 5 meters
        keyLight.DirectionalShadowPancakeSize = 0.5f; 
        keyLight.ShadowBias = 0.01f;         
        keyLight.ShadowNormalBias = 1.0f; 
        keyLight.ShadowBlur = 1.5f;
        
        keyLight.LightColor = new Color(1.0f, 0.98f, 0.95f);
        AddChild(keyLight);

        // Secondary Fill Light (To soften shadows)
        var fillLight = new DirectionalLight3D();
        fillLight.Name = "FillLight";
        fillLight.RotationDegrees = new Vector3(30, -135, 0); 
        fillLight.ShadowEnabled = false; 
        fillLight.LightEnergy = 0.4f; 
        fillLight.LightColor = new Color(0.95f, 0.98f, 1.0f);
        AddChild(fillLight);

        // World Environment
        var env = new WorldEnvironment();
        var sky = new Sky();
        var skyMat = new ProceduralSkyMaterial();
        skyMat.SkyTopColor = new Color(0.2f, 0.3f, 0.4f);
        skyMat.SkyHorizonColor = new Color(0.4f, 0.45f, 0.5f);
        sky.SkyMaterial = skyMat;
        
        var environment = new Godot.Environment();
        environment.BackgroundMode = Godot.Environment.BGMode.Sky;
        environment.Sky = sky;
        environment.AmbientLightSource = Godot.Environment.AmbientSource.Sky;
        environment.AmbientLightEnergy = 0.5f; 
        
        environment.SsaoEnabled = true;
        environment.SsaoIntensity = 2.0f;
        environment.SsaoRadius = RenderScale.ToWorld(50f); // 50mm (5cm) radius
        
        environment.SsilEnabled = true;
        environment.SsilIntensity = 1.0f;
        
        environment.TonemapMode = Godot.Environment.ToneMapper.Filmic;
        environment.TonemapExposure = 1.0f;
        
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
        _unitViews[unit] = view;
    }

    public void RollDicePool(Dictionary<string, int> pool)
    {
        if (pool.Count == 0) return;

        // Calculate a spawn area in front of the camera or at center
        var camera = GetViewport().GetCamera3D();
        Vector3 spawnCenter = Vector3.Zero;
        if (camera != null)
        {
            // Spawn ~300mm in front of camera on the ground
            Vector3 forward = -camera.GlobalTransform.Basis.Z;
            forward.Y = 0;
            if (forward.Length() > 0.1f)
            {
                spawnCenter = camera.GlobalPosition + forward.Normalized() * RenderScale.ToWorld(300f);
                spawnCenter.Y = RenderScale.ToWorld(50f); // 50mm high
            }
        }

        int totalDice = pool.Values.Sum();
        float spacing = RenderScale.ToWorld(40f);
        int cols = Mathf.CeilToInt(Mathf.Sqrt(totalDice));
        
        int current = 0;
        foreach (var entry in pool)
        {
            var def = Config.Dice.Find(d => d.Name == entry.Key);
            if (def == null) continue;

            for (int i = 0; i < entry.Value; i++)
            {
                var die = new Die(def.Name, def.Faces.ToArray());
                var dieView = new DieView(die);
                
                int r = current / cols;
                int c = current % cols;
                float jitter = RenderScale.ToWorld(5.0f); // 5mm jitter
                Vector3 offset = new Vector3(
                    (c - (cols-1)/2.0f) * spacing + (GD.Randf() - 0.5f) * jitter, 
                    0, 
                    (r - (cols-1)/2.0f) * spacing + (GD.Randf() - 0.5f) * jitter
                );
                
                dieView.Position = spawnCenter + offset;
                // Randomize initial rotation for variety
                dieView.Rotation = new Vector3(GD.Randf() * Mathf.Tau, GD.Randf() * Mathf.Tau, GD.Randf() * Mathf.Tau);
                
                AddChild(dieView);
                _diceViews.Add(dieView);
                dieView.StartRoll();
                current++;
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
    }
}
