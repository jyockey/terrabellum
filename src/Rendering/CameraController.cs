using Godot;

namespace Terrabellum.Rendering;

public partial class CameraController : Camera3D
{
    private float _panSpeed = 2.0f;
    private float _zoomSpeed = 20.0f;
    private float _minHeight = 100.0f;
    private float _maxHeight = 2000.0f;

    public override void _Ready()
    {
        // Initial setup for top-down view
        // We rely on Main.cs to call LookAt or set rotation for initial orientation
        Projection = ProjectionType.Perspective;
        Far = 4000.0f;
        Current = true;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Panning with Middle Mouse Button on XZ plane
        if (@event is InputEventMouseMotion mouseMotion && Input.IsMouseButtonPressed(MouseButton.Middle))
        {
            float factor = Position.Y / 500.0f;
            Vector3 delta = new Vector3(-mouseMotion.Relative.X * factor, 0, -mouseMotion.Relative.Y * factor);
            Position += delta;
        }

        // Zooming with Mouse Wheel
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                AdjustZoom(-_zoomSpeed);
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                AdjustZoom(_zoomSpeed);
            }
        }
    }

    private void AdjustZoom(float amount)
    {
        Vector3 newPos = Position;
        newPos.Y = Mathf.Clamp(newPos.Y + amount, _minHeight, _maxHeight);
        Position = newPos;
    }
}
