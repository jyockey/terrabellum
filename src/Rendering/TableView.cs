using Godot;

namespace Terrabellum.Rendering;

public partial class TableView : Node2D
{
    private Sprite2D _terrainSprite = new();

    public override void _Ready()
    {
        Name = "TableView";
        AddChild(_terrainSprite);
        
        // Ensure it draws behind everything else
        ZIndex = -100;

        // Default placeholder if no texture is found
        _terrainSprite.Texture = GD.Load<Texture2D>("res://assets/textures/terrain/default.jpg") ?? CreatePlaceholder();
        _terrainSprite.Centered = false;
    }

    private Texture2D CreatePlaceholder()
    {
        var image = Image.Create(2048, 2048, false, Image.Format.Rgba8);
        image.Fill(new Color(0.1f, 0.15f, 0.1f)); // Dark green "table"
        
        // Draw a simple noise or pattern if we wanted, but solid color is fine for now
        return ImageTexture.CreateFromImage(image);
    }
    
    public void SetTerrain(Texture2D texture)
    {
        _terrainSprite.Texture = texture;
    }
}
