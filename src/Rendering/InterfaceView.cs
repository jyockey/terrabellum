using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Terrabellum.Core;

namespace Terrabellum.Rendering;

public partial class InterfaceView : CanvasLayer
{
    public enum InteractionMode { Select, Move, Measure }
    
    private class InteractionModeDef
    {
        public InteractionMode Mode { get; init; }
        public string Icon { get; init; } = "";
        public string Tooltip { get; init; } = "";
        public Input.CursorShape Cursor { get; init; } = Input.CursorShape.Arrow;
    }

    private static readonly Dictionary<InteractionMode, InteractionModeDef> _modeDefinitions = new()
    {
        [InteractionMode.Select] = new() { Mode = InteractionMode.Select, Icon = "🖱️", Tooltip = "Selection Mode", Cursor = Input.CursorShape.Arrow },
        [InteractionMode.Move] = new() { Mode = InteractionMode.Move, Icon = "🖐️", Tooltip = "Movement Mode", Cursor = Input.CursorShape.PointingHand },
        [InteractionMode.Measure] = new() { Mode = InteractionMode.Measure, Icon = "📏", Tooltip = "Measurement Mode", Cursor = Input.CursorShape.Arrow }
    };

    public InteractionMode CurrentMode { get; private set; } = InteractionMode.Select;
    private Dictionary<InteractionMode, Button> _modeButtons = new();
    private readonly GameConfig _config;
    private readonly GameState _state;
    private Theme? _globalTheme;

    // Selection state
    private Unit? _selectedUnit;
    private PanelContainer _infoWindow = new();
    private VBoxContainer _infoContent = new();
    private ConsoleView _console = new();

    private Line2D _measureLine = new();
    private Label _measureLabel = new();
    private bool _isMeasuring = false;
    private Vector2 _measureStartPos;

    // Movement state
    private MovementPath? _activeMovement;
    private bool _isSelectingFacing = false;
    private Line2D _pathLine = new();
    private Label _pathLabel = new();

    public InterfaceView(GameConfig config, GameState state)
    {
        _config = config;
        _state = state;
    }

    public override void _Ready()
    {
        Name = "InterfaceView";
        SetupGlobalTheme();
        SetupUI();
        UpdateButtonVisuals();
        
        _console.CommandSubmitted += OnCommandSubmitted;
    }

    private void OnCommandSubmitted(string input)
    {
        string command = input.Trim();
        if (command.StartsWith("/")) command = command.Substring(1);
        
        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        string cmd = parts[0].ToLower();
        switch (cmd)
        {
            case "roll":
                if (parts.Length > 1) 
                {
                    // Combine all arguments: "roll 3 Attack" -> "3 Attack"
                    string expression = string.Join(" ", parts.Skip(1));
                    HandleRollCommand(expression);
                }
                else Log("[color=#ff8888]Usage: /roll <number><dice_name> (e.g. /roll 2d6)[/color]");
                break;
            default:
                Log($"[color=#ff8888]Unknown command: {cmd}[/color]");
                break;
        }
    }

    private void HandleRollCommand(string expression)
    {
        // Handle expressions like "2d6", "3 Attack", or just "Attack"
        // Regex looks for leading digits (optional), then optional space, then the rest of the string
        var match = System.Text.RegularExpressions.Regex.Match(expression.Trim(), @"^(\d+)?\s*(.*)$");
        if (!match.Success) return;

        string countStr = match.Groups[1].Value;
        int count = string.IsNullOrEmpty(countStr) ? 1 : int.Parse(countStr);
        string diceName = match.Groups[2].Value.Trim();

        // If diceName is empty (e.g. someone typed "/roll 3"), fall back to default d6
        if (string.IsNullOrEmpty(diceName)) diceName = "d6";

        var diceDef = _config.Dice.Find(d => d.Name.Equals(diceName, System.StringComparison.OrdinalIgnoreCase));
        if (diceDef == null)
        {
            Log($"[color=#ff8888]Error: Dice '{diceName}' not found in game config.[/color]");
            return;
        }

        var die = new Die(diceDef.Name, diceDef.Faces.ToArray());
        var results = new System.Collections.Generic.List<string>();
        for (int i = 0; i < count; i++)
        {
            die.Roll();
            results.Add(die.LastResultValue);
        }

        Log($"Rolled {count}{diceDef.Name}: [color=#ffffff]{string.Join(", ", results)}[/color]");
    }

    private void SetupGlobalTheme()
    {
        var fontPath = "res://assets/fonts/Roboto-VariableFont_wdth,wght.ttf";
        if (Godot.FileAccess.FileExists(fontPath))
        {
            var font = GD.Load<Font>(fontPath);
            _globalTheme = new Theme();
            _globalTheme.DefaultFont = font;
            _globalTheme.DefaultFontSize = 16;
            
            // Apply theme to our existing top-level control nodes
            _infoWindow.Theme = _globalTheme;
            _console.Theme = _globalTheme;
        }
    }

    private void SetupUI()
    {
        var margin = new MarginContainer();
        if (_globalTheme != null) margin.Theme = _globalTheme;
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft, Control.LayoutPresetMode.KeepSize, 20);
        AddChild(margin);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(hbox);

        foreach (var def in _modeDefinitions.Values)
        {
            hbox.AddChild(CreateModeButton(def));
        }

        // Measurement Visuals
        _measureLine.Width = 3.0f;
        _measureLine.DefaultColor = Colors.Yellow;
        AddChild(_measureLine);

        _measureLabel.AddThemeColorOverride("font_color", Colors.Yellow);
        _measureLabel.AddThemeFontSizeOverride("font_size", 18);
        _measureLabel.Hide();
        AddChild(_measureLabel);

        // Path Visuals
        _pathLine.Width = 3.0f;
        _pathLine.DefaultColor = Colors.Cyan;
        AddChild(_pathLine);

        _pathLabel.AddThemeColorOverride("font_color", Colors.Cyan);
        _pathLabel.AddThemeFontSizeOverride("font_size", 18);
        _pathLabel.Hide();
        AddChild(_pathLabel);

        // Info Window
        _infoWindow = new PanelContainer();
        _infoWindow.MouseFilter = Control.MouseFilterEnum.Stop;
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = new Color(0, 0, 0, 0.7f);
        styleBox.SetContentMarginAll(15);
        _infoWindow.AddThemeStyleboxOverride("panel", styleBox);
        
        _infoWindow.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _infoWindow.GrowHorizontal = Control.GrowDirection.Begin;
        _infoWindow.GrowVertical = Control.GrowDirection.End;
        
        // Margin from top-right corner
        _infoWindow.OffsetLeft = -20;
        _infoWindow.OffsetTop = 20;
        _infoWindow.OffsetRight = -20;
        _infoWindow.OffsetBottom = 20;
        
        _infoWindow.Hide();
        AddChild(_infoWindow);

        _infoContent = new VBoxContainer();
        _infoContent.MouseFilter = Control.MouseFilterEnum.Stop;
        _infoWindow.AddChild(_infoContent);

        // Console
        AddChild(_console);
    }

    public void Log(string message) => _console.AddEvent(message, _state.CurrentTurn);

    private Button CreateModeButton(InteractionModeDef def)
    {
        var btn = new Button();
        btn.Text = def.Icon;
        btn.TooltipText = def.Tooltip;
        btn.CustomMinimumSize = new Vector2(50, 50);
        btn.FocusMode = Control.FocusModeEnum.None;
        btn.Pressed += () => 
        {
            CurrentMode = def.Mode;
            StopMeasuring();
            CancelMovement();
            UpdateButtonVisuals();
        };
        _modeButtons[def.Mode] = btn;
        return btn;
    }

    private void UpdateButtonVisuals()
    {
        foreach (var kvp in _modeButtons)
        {
            kvp.Value.SelfModulate = kvp.Key == CurrentMode ? new Color(0.5f, 1.0f, 0.5f) : Colors.White;
        }

        if (_modeDefinitions.TryGetValue(CurrentMode, out var def))
        {
            Input.SetDefaultCursorShape(def.Cursor);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Slash)
        {
            _console.ActivateInput();
            GetViewport().SetInputAsHandled();
            return;
        }

        HandleSelectionInput(@event);
        HandleMeasurementInput(@event);
        HandleMovementInput(@event);
    }

    private void HandleSelectionInput(InputEvent @event)
    {
        if (CurrentMode != InteractionMode.Select) return;

        if (@event is InputEventMouseButton mouseBtn && mouseBtn.ButtonIndex == MouseButton.Left && mouseBtn.Pressed)
        {
            var camera = GetViewport().GetCamera3D();
            if (camera == null) return;

            Vector3 groundPos = GetGroundPos(camera, mouseBtn.Position);
            System.Numerics.Vector2 logicalPos = RenderScale.ToLogicalPos(groundPos);

            var unit = PickUnit(logicalPos);
            if (unit != null)
            {
                _selectedUnit = unit;
                UpdateInfoWindow();
            }
            else
            {
                _selectedUnit = null;
                _infoWindow.Hide();
            }
        }
    }

    private void UpdateInfoWindow()
    {
        if (_selectedUnit == null)
        {
            _infoWindow.Hide();
            return;
        }

        foreach (var child in _infoContent.GetChildren())
        {
            child.QueueFree();
        }

        var nameLabel = new Label();
        nameLabel.Text = _selectedUnit.CustomName;
        nameLabel.AddThemeFontSizeOverride("font_size", 20);
        _infoContent.AddChild(nameLabel);

        if (_selectedUnit.CustomName != _selectedUnit.Definition.Name)
        {
            var typeLabel = new Label();
            typeLabel.Text = $"({_selectedUnit.Definition.Name})";
            typeLabel.SelfModulate = new Color(0.8f, 0.8f, 0.8f);
            _infoContent.AddChild(typeLabel);
        }

        _infoContent.AddChild(new HSeparator());

        foreach (var stat in _selectedUnit.CurrentStats)
        {
            var hbox = new HBoxContainer();
            var keyLabel = new Label { Text = stat.Key + ": " };
            var valLabel = new Label { Text = stat.Value.ToString() };
            hbox.AddChild(keyLabel);
            hbox.AddChild(valLabel);
            _infoContent.AddChild(hbox);
        }

        _infoWindow.Show();
    }

    private void HandleMovementInput(InputEvent @event)
    {
        if (CurrentMode != InteractionMode.Move) return;

        var camera = GetViewport().GetCamera3D();
        if (camera == null) return;

        if (@event is InputEventMouseButton mouseBtn && mouseBtn.ButtonIndex == MouseButton.Left && mouseBtn.Pressed)
        {
            if (_isSelectingFacing)
            {
                // Finalize rotation and movement
                if (_activeMovement != null)
                {
                    float dist = _activeMovement.GetTotalDistance(_activeMovement.Unit.Position) / _config.UnitsPerMeasurement;
                    Log($"[color=#ffffaa]{_activeMovement.Unit.CustomName}[/color] moved [color=#88ff88]{dist:F1}{_config.UnitSuffix}[/color]");
                    _activeMovement.Finalize(_activeMovement.Unit.Position);
                }
                CancelMovement();
                return;
            }

            Vector3 groundPos = GetGroundPos(camera, mouseBtn.Position);
            System.Numerics.Vector2 logicalPos = RenderScale.ToLogicalPos(groundPos);

            if (_activeMovement == null)
            {
                var unit = PickUnit(logicalPos);
                if (unit != null)
                {
                    _selectedUnit = unit;
                    UpdateInfoWindow();
                    _activeMovement = new MovementPath(unit);
                }
            }
            else
            {
                if (mouseBtn.DoubleClick)
                {
                    _isSelectingFacing = true;
                }
                else
                {
                    // Only add waypoint if it doesn't cause a collision
                    if (!IsCollisionAt(_activeMovement.Unit, logicalPos))
                    {
                        _activeMovement.AddWaypoint(logicalPos);
                    }
                }
            }
        }

        if (@event is InputEventMouseMotion mouseMotion && _activeMovement != null)
        {
            Vector3 groundPos = GetGroundPos(camera, mouseMotion.Position);
            System.Numerics.Vector2 logicalPos = RenderScale.ToLogicalPos(groundPos);
            
            if (_isSelectingFacing)
            {
                // Rotate towards mouse
                var diff = logicalPos - _activeMovement.Unit.Position;
                if (diff.Length() > 1.0f) // 1mm
                {
                    // Use Atan2(x, -y) to make 0 = North (-Z) and CW = positive
                    _activeMovement.Unit.Rotation = Mathf.Atan2(diff.X, -diff.Y);
                }
            }
            else
            {
                if (!IsCollisionAt(_activeMovement.Unit, logicalPos))
                {
                    _activeMovement.Unit.Position = logicalPos;
                }
            }
            UpdatePathVisuals(camera, _activeMovement.Unit.Position);
        }
    }

    private bool IsCollisionAt(Unit unit, System.Numerics.Vector2 pos)
    {
        var spaceState = GetViewport().World3D.DirectSpaceState;
        
        var main = GetTree().Root.GetNodeOrNull<Main>("Main");
        if (main == null) return false;

        UnitView? movingView = main.GetUnitView(unit);
        if (movingView == null) return false;

        var shape = movingView.GetNodeOrNull<CollisionShape3D>("StaticBody3D/CollisionShape3D");
        if (shape == null || shape.Shape == null) return false;

        var query = new PhysicsShapeQueryParameters3D();
        query.Shape = shape.Shape;
        
        // Ensure we check at the correct logical height for base (e.g. 1mm up)
        query.Transform = new Transform3D(Basis.Identity, RenderScale.ToWorldPos(pos, RenderScale.ToWorld(1.0f)));
        
        // Exclude the moving unit's own body
        var body = movingView.GetNodeOrNull<StaticBody3D>("StaticBody3D");
        if (body != null) query.Exclude = new Godot.Collections.Array<Rid> { body.GetRid() };

        var result = spaceState.IntersectShape(query, 1);
        return result.Count > 0;
    }

    private void UpdatePathVisuals(Camera3D camera, System.Numerics.Vector2 terminalPos)
    {
        if (_activeMovement == null) return;

        var screenPoints = new List<Vector2>();
        foreach (var wp in _activeMovement.Waypoints)
        {
            screenPoints.Add(camera.UnprojectPosition(RenderScale.ToWorldPos(wp)));
        }
        screenPoints.Add(camera.UnprojectPosition(RenderScale.ToWorldPos(terminalPos)));


        _pathLine.Points = screenPoints.ToArray();
        
        // Logical units are still mm, so convert to measurement (e.g. inches)
        float distance = _activeMovement.GetTotalDistance(terminalPos) / _config.UnitsPerMeasurement;
        string text = $"{distance:F1}{_config.UnitSuffix}";
        if (_isSelectingFacing) text += " (Set Facing)";
        _pathLabel.Text = text;
        _pathLabel.Position = screenPoints[screenPoints.Count - 1] + new Vector2(15, 15);
        _pathLabel.Show();
    }

    private void CancelMovement()
    {
        _activeMovement = null;
        _isSelectingFacing = false;
        _pathLine.ClearPoints();
        _pathLabel.Hide();
    }

    private Vector3 GetGroundPos(Camera3D camera, Vector2 mousePos)
    {
        Vector3 origin = camera.ProjectRayOrigin(mousePos);
        Vector3 normal = camera.ProjectRayNormal(mousePos);
        if (Mathf.Abs(normal.Y) < 0.0001f) return Vector3.Zero;
        float t = -origin.Y / normal.Y;
        return origin + normal * t;
    }

    private Unit? PickUnit(System.Numerics.Vector2 pos)
    {
        var main = GetTree().Root.GetNodeOrNull<Main>("Main");
        if (main == null) return null;

        foreach (var unit in main.Tabletop.Units)
        {
            if ((unit.Position - pos).Length() <= unit.Definition.BaseSize / 2.0f)
                return unit;
        }
        return null;
    }

    private void HandleMeasurementInput(InputEvent @event)
    {
        if (CurrentMode != InteractionMode.Measure) return;

        if (@event is InputEventMouseButton mouseBtn && mouseBtn.ButtonIndex == MouseButton.Left)
        {
            if (mouseBtn.Pressed)
            {
                _isMeasuring = true;
                _measureStartPos = mouseBtn.Position;
                _measureLine.ClearPoints();
                _measureLine.AddPoint(_measureStartPos);
                _measureLine.AddPoint(_measureStartPos);
                _measureLabel.Show();
            }
            else
            {
                StopMeasuring();
            }
        }

        if (@event is InputEventMouseMotion mouseMotion && _isMeasuring)
        {
            if (_measureLine.GetPointCount() > 1)
                _measureLine.SetPointPosition(1, mouseMotion.Position);
            
            var camera = GetViewport().GetCamera3D();
            if (camera != null)
            {
                Vector3 startWorld = GetGroundPos(camera, _measureStartPos);
                Vector3 endWorld = GetGroundPos(camera, mouseMotion.Position);
                // Convert world distance back to logical distance before dividing by config units
                float dist = RenderScale.ToLogical((endWorld - startWorld).Length()) / _config.UnitsPerMeasurement;
                _measureLabel.Text = $"{dist:F1}{_config.UnitSuffix}";
            }
            else
            {
                float dist = _measureStartPos.DistanceTo(mouseMotion.Position);
                _measureLabel.Text = $"{(int)dist} px";
            }
            
            _measureLabel.Position = mouseMotion.Position + new Vector2(15, 15);
        }
    }

    private void StopMeasuring()
    {
        _isMeasuring = false;
        _measureLine.ClearPoints();
        _measureLabel.Hide();
    }
}
