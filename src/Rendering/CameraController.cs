using Godot;

namespace Terrabellum.Rendering;

public partial class CameraController : Camera2D
{
    private float _zoomSpeed = 0.1f;
    private float _minZoom = 0.1f;
    private float _maxZoom = 5.0f;

    public override void _Ready()
    {
        // Set initial zoom
        Zoom = Vector2.One;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Panning with Middle Mouse Button
        if (@event is InputEventMouseMotion mouseMotion && Input.IsMouseButtonPressed(MouseButton.Middle))
        {
            Position -= mouseMotion.Relative / Zoom;
        }

        // Zooming with Mouse Wheel
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                AdjustZoom(1.0f + _zoomSpeed);
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                AdjustZoom(1.0f - _zoomSpeed);
            }
        }
    }

    private void AdjustZoom(float factor)
    {
        Vector2 mousePos = GetGlobalMousePosition();
        
        Vector2 newZoom = Zoom * factor;
        newZoom = newZoom.Clamp(Vector2.One * _minZoom, Vector2.One * _maxZoom);
        
        Zoom = newZoom;

        // Reposition camera so the mouse stays over the same world-space coordinate
        Vector2 newMousePos = GetGlobalMousePosition();
        Position += (mousePos - newMousePos);
    }
}
