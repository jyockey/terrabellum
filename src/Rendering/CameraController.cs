using Godot;

namespace Terrabellum.Rendering;

public partial class CameraController : Camera3D
{
    private float _panSpeed = 2.0f; // meters per second
    private float _zoomSpeed = 0.1f;
    private float _rotationSensitivity = 0.005f;
    
    // Limits derived from unit scale
    private float _minHeight = RenderScale.ToWorld(RenderScale.StandardUnitHeight / 2.0f);
    private float _maxHeight = RenderScale.ToWorld(RenderScale.StandardUnitHeight * 100.0f);

    private float _yaw = 0.0f;
    private float _pitch = -Mathf.Pi / 4.0f;

    public override void _Ready()
    {
        Projection = ProjectionType.Perspective;
        Far = RenderScale.ToWorld(RenderScale.StandardUnitHeight * 2000.0f); // ~50m
        Current = true;

        _yaw = Rotation.Y;
        _pitch = Rotation.X;
    }

    public override void _Process(double delta)
    {
        HandleKeyboardPanning((float)delta);
    }

    private void HandleKeyboardPanning(float delta)
    {
        Vector3 moveDir = Vector3.Zero;
        Vector3 forward = new Vector3(-Mathf.Sin(_yaw), 0, -Mathf.Cos(_yaw));
        Vector3 right = new Vector3(Mathf.Cos(_yaw), 0, -Mathf.Sin(_yaw));

        if (Input.IsKeyPressed(Key.W)) moveDir += forward;
        if (Input.IsKeyPressed(Key.S)) moveDir -= forward;
        if (Input.IsKeyPressed(Key.A)) moveDir -= right;
        if (Input.IsKeyPressed(Key.D)) moveDir += right;

        if (moveDir != Vector3.Zero)
        {
            moveDir = moveDir.Normalized();
            // Feel factor based on standard unit height
            float heightFactor = Mathf.Clamp(Position.Y / 0.5f, 0.5f, 4.0f);
            Position += moveDir * _panSpeed * heightFactor * delta;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion && Input.IsMouseButtonPressed(MouseButton.Middle))
        {
            _yaw -= mouseMotion.Relative.X * _rotationSensitivity;
            _pitch -= mouseMotion.Relative.Y * _rotationSensitivity;
            _pitch = Mathf.Clamp(_pitch, -Mathf.Pi / 2.0f + 0.1f, -0.05f); 
            Rotation = new Vector3(_pitch, _yaw, 0);
        }

        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.WheelUp) AdjustZoom(-_zoomSpeed);
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown) AdjustZoom(_zoomSpeed);
        }
    }

    private void AdjustZoom(float amount)
    {
        float zoomFactor = Mathf.Clamp(Position.Y / 0.5f, 0.1f, 5.0f);
        Vector3 newPos = Position + GlobalTransform.Basis.Z * amount * zoomFactor;
        newPos.Y = Mathf.Clamp(newPos.Y, _minHeight, _maxHeight);
        Position = newPos;
    }
}
