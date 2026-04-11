using Godot;
using System.Collections.Generic;
using Terrabellum.Core;

namespace Terrabellum.Rendering;

public partial class InterfaceView : CanvasLayer
{
    public enum InteractionMode { Move, Measure }
    public InteractionMode CurrentMode { get; private set; } = InteractionMode.Move;
    private Dictionary<InteractionMode, Button> _modeButtons = new();
    private readonly GameConfig _config;

    private Line2D _measureLine = new();
    private Label _measureLabel = new();
    private bool _isMeasuring = false;
    private Vector2 _measureStartPos;

    // Movement state
    private MovementPath? _activeMovement;
    private bool _isSelectingFacing = false;
    private Line2D _pathLine = new();
    private Label _pathLabel = new();

    public InterfaceView(GameConfig config)
    {
        _config = config;
    }

    public override void _Ready()
    {
        Name = "InterfaceView";
        SetupUI();
        UpdateButtonVisuals();
    }

    private void SetupUI()
    {
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft, Control.LayoutPresetMode.KeepSize, 20);
        AddChild(margin);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 10);
        margin.AddChild(hbox);

        hbox.AddChild(CreateModeButton("🖐️", "Movement Mode", InteractionMode.Move));
        hbox.AddChild(CreateModeButton("📏", "Measurement Mode", InteractionMode.Measure));

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
    }

    private Button CreateModeButton(string text, string tooltip, InteractionMode mode)
    {
        var btn = new Button();
        btn.Text = text;
        btn.TooltipText = tooltip;
        btn.CustomMinimumSize = new Vector2(50, 50);
        btn.Pressed += () => 
        {
            CurrentMode = mode;
            StopMeasuring();
            CancelMovement();
            UpdateButtonVisuals();
        };
        _modeButtons[mode] = btn;
        return btn;
    }

    private void UpdateButtonVisuals()
    {
        foreach (var kvp in _modeButtons)
        {
            kvp.Value.SelfModulate = kvp.Key == CurrentMode ? new Color(0.5f, 1.0f, 0.5f) : Colors.White;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        HandleMeasurementInput(@event);
        HandleMovementInput(@event);
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
                _activeMovement?.Finalize(_activeMovement.Unit.Position);
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
            if ((unit.Position - pos).Length() <= unit.Definition.BaseSize)
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
