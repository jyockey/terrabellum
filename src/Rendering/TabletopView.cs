using Godot;

namespace Terrabellum.Rendering;

public partial class TabletopView : Control
{
    private CameraController? _camera;
    private bool _w, _a, _s, _d;

    public void Setup(CameraController camera)
    {
        _camera = camera;
    }

    public override void _Ready()
    {
        Name = "TabletopView";
        SetAnchorsPreset(LayoutPreset.FullRect);
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Pass; // Allow events to fall through to _UnhandledInput for selection
        
        // Connect to viewport size changes to ensure we always cover the screen
        GetViewport().SizeChanged += OnViewportResized;
        OnViewportResized();

        // Ensure we start with focus so WASD works immediately
        GrabFocus();
    }

    private void OnViewportResized()
    {
        Size = GetViewportRect().Size;
    }

    public override void _GuiInput(InputEvent @event)
    {
        // Handle Camera Panning Keys
        if (@event is InputEventKey keyEvent && !keyEvent.Echo)
        {
            bool handled = false;
            switch (keyEvent.Keycode)
            {
                case Key.W: _w = keyEvent.Pressed; handled = true; break;
                case Key.S: _s = keyEvent.Pressed; handled = true; break;
                case Key.A: _a = keyEvent.Pressed; handled = true; break;
                case Key.D: _d = keyEvent.Pressed; handled = true; break;
            }
            
            if (handled)
            {
                AcceptEvent();
                return;
            }
        }

        // Handle Camera Rotation (Middle Mouse)
        if (@event is InputEventMouseMotion mouseMotion && Input.IsMouseButtonPressed(MouseButton.Middle))
        {
            _camera?.Rotate(mouseMotion.Relative);
            AcceptEvent();
            return;
        }

        // Handle Camera Zoom (Scroll)
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                _camera?.Zoom(-1.0f);
                AcceptEvent();
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                _camera?.Zoom(1.0f);
                AcceptEvent();
            }

            // Clicking the tabletop should reclaim focus from other UI elements
            if (mouseButton.Pressed)
            {
                GrabFocus();
            }
        }
    }

    public override void _Process(double delta)
    {
        if (_camera == null) return;

        var input = new Vector2(
            (_d ? 1 : 0) - (_a ? 1 : 0),
            (_w ? 1 : 0) - (_s ? 1 : 0)
        );

        if (input != Vector2.Zero)
        {
            _camera.Pan(input, (float)delta);
        }
    }

    public override void _Notification(int what)
    {
        // Safety: If we lose focus (e.g. Alt-Tab or Console open), stop movement
        if (what == NotificationFocusExit || what == NotificationApplicationFocusOut)
        {
            _w = _a = _s = _d = false;
        }
    }
}
