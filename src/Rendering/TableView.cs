using Godot;

namespace Terrabellum.Rendering;

public partial class TableView : Node3D
{
    private MeshInstance3D _terrainMesh = new();

    public override void _Ready()
    {
        Name = "TableView";
        AddChild(_terrainMesh);
        
        var plane = new PlaneMesh();
        plane.Size = new Vector2(2048, 2048);
        _terrainMesh.Mesh = plane;

        var material = new StandardMaterial3D();
        material.AlbedoTexture = GD.Load<Texture2D>("res://assets/textures/terrain/default.jpg") ?? CreatePlaceholder();
        _terrainMesh.SetSurfaceOverrideMaterial(0, material);
    }

    private Texture2D CreatePlaceholder()
    {
        var image = Image.CreateEmpty(2048, 2048, false, Image.Format.Rgba8);
        image.Fill(new Color(0.1f, 0.15f, 0.1f)); 
        return ImageTexture.CreateFromImage(image);
    }
    
    public void SetTerrain(Texture2D texture)
    {
        if (_terrainMesh.GetSurfaceOverrideMaterial(0) is StandardMaterial3D mat)
        {
            mat.AlbedoTexture = texture;
        }
    }
}
