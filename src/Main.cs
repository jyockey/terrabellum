using Godot;
using System.Collections.Generic;
using Terrabellum.Core;
using Terrabellum.Rendering;

namespace Terrabellum;

public partial class Main : Node
{
    private Tabletop _tabletop = new();
    private List<UnitView> _unitViews = new();
    private List<DieView> _diceViews = new();

    public override void _Ready()
    {
        GD.Print("Terrabellum Initializing...");

        // Setup Table
        var tableView = new TableView();
        AddChild(tableView);

        // Setup Camera
        var camera = new CameraController();
        AddChild(camera);

        // Dice Setup
        for (int i = 0; i < 3; i++)
        {
            var die = new Die(6);
            var dieView = new DieView(die);
            dieView.Position = new Vector2(50 + (i * 60), 600);
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
        SpawnUnit(orcDefinition, new System.Numerics.Vector2(100, 100), Colors.Green);
        SpawnUnit(marineDefinition, new System.Numerics.Vector2(300, 100), Colors.Blue);
        SpawnUnit(tankDefinition, new System.Numerics.Vector2(200, 300), Colors.Blue);
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
