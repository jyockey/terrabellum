using System.Numerics;

namespace Terrabellum.Core;

public class ModelDefinition
{
    public string Path { get; set; } = string.Empty;
    public float Scale { get; set; } = 1.0f;
    public float Rotation { get; set; } = 0.0f;
    public Vector3 Offset { get; set; } = Vector3.Zero;
}
