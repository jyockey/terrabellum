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
        Fov = 50.0f; // Narrower FOV reduces perspective skewing
        Near = 0.001f; // 1mm near clip for close-up inspection
        Far = RenderScale.ToWorld(RenderScale.StandardUnitHeight * 2000.0f); // ~50m
        Current = true;

        _yaw = Rotation.Y;
        _pitch = Rotation.X;
    }

    public void Pan(Vector2 input, float delta)
    {
        Vector3 moveDir = Vector3.Zero;
        Vector3 forward = new Vector3(-Mathf.Sin(_yaw), 0, -Mathf.Cos(_yaw));
        Vector3 right = new Vector3(Mathf.Cos(_yaw), 0, -Mathf.Sin(_yaw));

        moveDir += forward * input.Y; // W/S
        moveDir += right * input.X;   // A/D

        if (moveDir != Vector3.Zero)
        {
            moveDir = moveDir.Normalized();
            // Feel factor based on standard unit height
            float heightFactor = Mathf.Clamp(Position.Y / 0.5f, 0.5f, 4.0f);
            Position += moveDir * _panSpeed * heightFactor * delta;
        }
    }

    public void Rotate(Vector2 relative)
    {
        _yaw -= relative.X * _rotationSensitivity;
        _pitch -= relative.Y * _rotationSensitivity;
        _pitch = Mathf.Clamp(_pitch, -Mathf.Pi / 2.0f + 0.1f, -0.05f); 
        Rotation = new Vector3(_pitch, _yaw, 0);
    }

    public void Zoom(float direction)
    {
        float zoomFactor = Mathf.Clamp(Position.Y / 0.5f, 0.1f, 5.0f);
        Vector3 newPos = Position + GlobalTransform.Basis.Z * direction * _zoomSpeed * zoomFactor;
        newPos.Y = Mathf.Clamp(newPos.Y, _minHeight, _maxHeight);
        Position = newPos;
    }
}
