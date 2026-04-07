using Godot;
using Terrabellum.Core;
using System.Collections.Generic;
using System.Text.Json;

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

public partial class DieView : Node3D
{
    private static Dictionary<string, List<FaceMetadata>>? _metadata;
    private readonly Die _die;
    private bool _isRolling;
    private double _rollTimer;
    private double _rollDuration = 0.6;
    private double _tickTimer;
    private double _tickInterval = 0.05;

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
            _metadata = JsonSerializer.Deserialize<Dictionary<string, List<FaceMetadata>>>(json, options);
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
        
        if (customMesh != null)
        {
            _mesh.Mesh = customMesh;
        }
        else
        {
            _mesh.Mesh = new BoxMesh { Size = new Vector3(40, 40, 40) };
            _mesh.Position = new Vector3(0, 20, 0);
        }

        var material = new StandardMaterial3D 
        { 
            VertexColorUseAsAlbedo = true,
            AlbedoColor = new Color(0.7f, 0.7f, 0.7f),
            Roughness = 0.2f,
            RimEnabled = true,
            Rim = 0.5f,
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

        var faces = _metadata[modelName];
        for (int i = 0; i < faces.Count; i++)
        {
            var face = faces[i];
            var normal = new Vector3(face.Normal[0], face.Normal[1], face.Normal[2]);
            foreach (var labelMeta in face.Labels)
            {
                var pos = new Vector3(labelMeta.Pos[0], labelMeta.Pos[1], labelMeta.Pos[2]);
                var up = new Vector3(labelMeta.Up[0], labelMeta.Up[1], labelMeta.Up[2]);
                
                string text = labelMeta.Text;
                if (labelMeta.VertexIdx.HasValue)
                {
                    // For d4, result is based on vertex index
                    if (labelMeta.VertexIdx.Value < _die.Faces.Length)
                    {
                        text = _die.Faces[labelMeta.VertexIdx.Value];
                    }
                }
                else
                {
                    // For others, result is based on face index
                    if (i < _die.Faces.Length)
                    {
                        text = _die.Faces[i];
                    }
                }
                
                AddFaceLabel(text, pos, normal, up);
            }
        }
    }

    private void AddFaceLabel(string text, Vector3 position, Vector3 normal, Vector3 upVector)
    {
        var label = new Label3D();
        label.Text = text;
        label.FontSize = 128;
        label.PixelSize = 0.15f; 
        label.Modulate = Colors.Black;
        label.OutlineModulate = Colors.White;
        
        label.Basis = Basis.LookingAt(normal, upVector, true);
        label.Position = position;

        _mesh.AddChild(label);
    }

    public void StartRoll()
    {
        _die.Roll();
        _isRolling = true;
        _rollTimer = _rollDuration;
    }

    public override void _Process(double delta)
    {
        if (!_isRolling) return;
        _rollTimer -= delta;

        if (_rollTimer <= 0)
        {
            _isRolling = false;
            SnapToFace();
            return;
        }

        _tickTimer -= delta;
        if (_tickTimer <= 0)
        {
            _tickTimer = _tickInterval;
            _mesh.RotationDegrees = new Vector3(GD.Randf() * 360, GD.Randf() * 360, GD.Randf() * 360);
        }
    }

    private void SnapToFace()
    {
        string modelName = $"d{_die.Sides}";
        if (_metadata == null || !_metadata.ContainsKey(modelName)) return;

        var faces = _metadata[modelName];
        if (_die.LastResultIndex >= faces.Count) return;

        // For all dice except D4, the 'result' is the face pointing UP.
        // For D4, our generator associates face index with result vertex index.
        // Pointing the face normal UP makes the die land on that face (if it's the bottom face).
        // My D4 generator: face 0 is the bottom face (Normal points DOWN). 
        // If result is 1 (index 0), we want face 0 normal pointing DOWN.
        
        var face = faces[_die.LastResultIndex];
        var targetNormal = new Vector3(face.Normal[0], face.Normal[1], face.Normal[2]);
        
        Vector3 worldTarget = (modelName == "d4") ? Vector3.Down : Vector3.Up;

        if (targetNormal.IsEqualApprox(worldTarget))
        {
            _mesh.Basis = Basis.Identity;
        }
        else if (targetNormal.IsEqualApprox(-worldTarget))
        {
            _mesh.Basis = new Basis(Vector3.Right, Mathf.Pi);
        }
        else
        {
            Vector3 axis = targetNormal.Cross(worldTarget).Normalized();
            float angle = Mathf.Acos(targetNormal.Dot(worldTarget));
            _mesh.Basis = new Basis(axis, angle);
        }
    }
}
