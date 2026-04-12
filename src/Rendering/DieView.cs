using Godot;
using Terrabellum.Core;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace Terrabellum.Rendering;

public class FaceLabelMetadata
{
    public string Text { get; set; } = string.Empty;
    public int? VertexIdx { get; set; }
    public float[] Pos { get; set; } = new float[3];
    public float[] Up { get; set; } = new float[3];
}

public class FaceMetadata
{
    public float[] Normal { get; set; } = new float[3];
    public List<FaceLabelMetadata> Labels { get; set; } = new();
}

public class DieMetadata
{
    public float F2fScale { get; set; } = 1.0f;
    public bool IsBottomResult { get; set; } = false;
    public List<FaceMetadata> Faces { get; set; } = new();
}

public partial class DieView : Node3D
{
    private static Dictionary<string, DieMetadata>? _metadata;
    private readonly Die _die;
    private bool _isRolling;
    private double _rollTimer;
    private double _rollDuration = 0.6;
    
    private Vector3 _velocity;
    private Vector3 _angularVelocity;
    private const float Gravity = -15.0f;
    private const float GroundY = 0f;

    private MeshInstance3D _mesh = new();

    public DieView(Die die)
    {
        _die = die;
        LoadMetadata();
    }

    private void LoadMetadata()
    {
        if (_metadata != null) return;
        try
        {
            string path = ProjectSettings.GlobalizePath("res://assets/models/dice/metadata.json");
            string json = System.IO.File.ReadAllText(path);
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
            };
            _metadata = JsonSerializer.Deserialize<Dictionary<string, DieMetadata>>(json, options);
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"Failed to load dice metadata: {e.Message}");
            _metadata = new();
        }
    }

    public override void _Ready()
    {
        AddChild(_mesh);
        
        string modelName = $"d{_die.Sides}";
        string meshPath = $"res://assets/models/dice/{modelName}.obj";
        var customMesh = GD.Load<Mesh>(meshPath);
        
        float worldScale = RenderScale.ToWorld(RenderScale.DiceSize);
        if (customMesh != null)
        {
            _mesh.Mesh = customMesh;
            _mesh.Scale = new Vector3(worldScale, worldScale, worldScale);
        }
        else
        {
            _mesh.Mesh = new BoxMesh { Size = new Vector3(worldScale, worldScale, worldScale) };
        }

        var material = new StandardMaterial3D 
        { 
            VertexColorUseAsAlbedo = true,
            AlbedoColor = new Color(0.7f, 0.7f, 0.7f),
            Roughness = 0.2f,
            DiffuseMode = StandardMaterial3D.DiffuseModeEnum.Lambert,
            SpecularMode = StandardMaterial3D.SpecularModeEnum.SchlickGgx
        };

        _mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
        _mesh.SetSurfaceOverrideMaterial(0, material);

        SetupFaces(modelName);
    }

    private void SetupFaces(string modelName)
    {
        if (_metadata == null || !_metadata.ContainsKey(modelName)) return;

        var data = _metadata[modelName];
        for (int i = 0; i < data.Faces.Count; i++)
        {
            var face = data.Faces[i];
            var normal = new Vector3(face.Normal[0], face.Normal[1], face.Normal[2]);
            foreach (var labelMeta in face.Labels)
            {
                var pos = new Vector3(labelMeta.Pos[0], labelMeta.Pos[1], labelMeta.Pos[2]);
                var up = new Vector3(labelMeta.Up[0], labelMeta.Up[1], labelMeta.Up[2]);
                
                string text = labelMeta.Text;
                if (labelMeta.VertexIdx.HasValue)
                {
                    if (labelMeta.VertexIdx.Value < _die.Faces.Length)
                        text = _die.Faces[labelMeta.VertexIdx.Value];
                }
                else
                {
                    if (i < _die.Faces.Length)
                        text = _die.Faces[i];
                }
                
                AddFaceLabel(text, pos, normal, up);
            }
        }
    }

    private void AddFaceLabel(string text, Vector3 position, Vector3 normal, Vector3 upVector)
    {
        var label = new Label3D();
        label.Text = text;
        label.FontSize = RenderScale.StandardFontSize;
        label.PixelSize = RenderScale.GetLocalPixelSize(RenderScale.DiceLabelHeight, RenderScale.DiceSize);
        label.Modulate = Colors.Black;
        label.OutlineModulate = Colors.White;
        label.OutlineSize = RenderScale.StandardOutlineSize;
        label.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        label.Basis = Basis.LookingAt(normal, upVector, true);
        label.Position = position + normal * 0.02f;

        _mesh.AddChild(label);
    }

    public void StartRoll()
    {
        _die.Roll();
        _isRolling = true;
        _rollTimer = _rollDuration;
        
        // Tuned for more realism: less "pop" and slower spin
        _velocity = new Vector3((GD.Randf() - 0.5f) * 1.2f, 2.5f, (GD.Randf() - 0.5f) * 1.2f);
        _angularVelocity = new Vector3(
            (GD.Randf() - 0.5f) * 15f, 
            (GD.Randf() - 0.5f) * 15f, 
            (GD.Randf() - 0.5f) * 15f
        );
    }

    public override void _Process(double delta)
    {
        if (!_isRolling) return;
        
        float fDelta = (float)delta;
        _rollTimer -= delta;

        if (_rollTimer <= 0)
        {
            _isRolling = false;
            SnapToFace();
            return;
        }

        // Apply visual physics
        _velocity.Y += Gravity * fDelta;
        Position += _velocity * fDelta;

        // Ground bounce with energy loss
        float worldScale = RenderScale.ToWorld(RenderScale.DiceSize);
        float radius = worldScale * 0.5f;
        if (Position.Y < GroundY + radius && _velocity.Y < 0)
        {
            Position = new Vector3(Position.X, GroundY + radius, Position.Z);
            _velocity.Y *= -0.3f; // More energy loss
            _velocity.X *= 0.6f; // More friction
            _velocity.Z *= 0.6f;
            _angularVelocity *= 0.7f;
        }

        // Apply rotation
        if (_angularVelocity.Length() > 0.001f)
        {
            _mesh.Rotate(_angularVelocity.Normalized(), _angularVelocity.Length() * fDelta);
        }
    }

    private void SnapToFace()
    {
        string modelName = $"d{_die.Sides}";
        if (_metadata == null || !_metadata.ContainsKey(modelName)) return;

        var data = _metadata[modelName];
        if (_die.LastResultIndex >= data.Faces.Count) return;

        var face = data.Faces[_die.LastResultIndex];
        var targetNormal = new Vector3(face.Normal[0], face.Normal[1], face.Normal[2]);
        
        // Align result face normal with target world vector based on metadata
        Vector3 worldTarget = data.IsBottomResult ? Vector3.Down : Vector3.Up;

        Basis targetRotation;
        if (targetNormal.IsEqualApprox(worldTarget))
        {
            targetRotation = Basis.Identity;
        }
        else if (targetNormal.IsEqualApprox(-worldTarget))
        {
            targetRotation = new Basis(Vector3.Right, Mathf.Pi);
        }
        else
        {
            Vector3 axis = targetNormal.Cross(worldTarget).Normalized();
            float angle = Mathf.Acos(Mathf.Clamp(targetNormal.Dot(worldTarget), -1.0f, 1.0f));
            targetRotation = new Basis(axis, angle);
        }

        float worldScale = RenderScale.ToWorld(RenderScale.DiceSize);
        _mesh.Basis = targetRotation.Scaled(new Vector3(worldScale, worldScale, worldScale));

        // Use scale factor from metadata to calculate resting height offset
        float halfHeight = (worldScale * data.F2fScale) / 2.0f;
        Position = new Vector3(Position.X, GroundY + halfHeight, Position.Z);
    }
}