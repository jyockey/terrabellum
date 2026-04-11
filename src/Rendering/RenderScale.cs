using Godot;

namespace Terrabellum.Rendering;

/// <summary>
/// Centralized scale manager using StandardBaseWidth as the root physical constant.
/// Dimensions are derived as proportions of the base width to ensure consistent 
/// visual weight across the tabletop.
/// </summary>
public static class RenderScale
{
    // The Atomic Scale: 1.0 in Godot = 1 meter. 1.0 in Core = 1 millimeter.
    public const float LogicToWorldScale = 0.001f;

    // --- Root Physical Constants (Logical mm) ---
    public const float StandardBaseWidth = 32.0f; // Primary root for game scale
    public const float StandardUnitHeight = 28.0f; 

    // --- Derived Aesthetic Constants (Logical mm) ---
    // These are derived from StandardBaseWidth to maintain visual proportions
    public const float BaseHeight = StandardBaseWidth / 16.0f;      // 2.0mm
    public const float DiceSize = StandardBaseWidth * 0.375f;       // 12.0mm Standard (face-to-face for d6)
    public const float IndicatorSize = StandardBaseWidth / 8.0f;    // 4.0mm
    public const float LabelOffset = StandardBaseWidth / 3.2f;      // 10.0mm
    
    // --- Text Scaling ---
    public const int StandardFontSize = 48; 
    public const int StandardOutlineSize = 4;
    public static readonly float UnitLabelHeight = StandardUnitHeight / 8.0f; // ~3.5mm
    public static readonly float DiceLabelHeight = DiceSize / 2.5f;           // ~4.8mm

    // --- World Space Shortcuts ---
    public static readonly float WorldBaseHeight = ToWorld(BaseHeight);
    public static readonly Vector3 ModelScale = new Vector3(LogicToWorldScale, LogicToWorldScale, LogicToWorldScale);
    
    /// <summary>Converts a logical dimension (mm) to a world dimension.</summary>
    public static float ToWorld(float logicalDistance) => logicalDistance * LogicToWorldScale;

    /// <summary>Converts a world dimension to a logical dimension (mm).</summary>
    public static float ToLogical(float worldDistance) => worldDistance / LogicToWorldScale;

    /// <summary>Calculates the required PixelSize to achieve a specific logical height (mm) at a given FontSize.</summary>
    public static float GetPixelSize(float logicalHeight, int fontSize = StandardFontSize) 
        => ToWorld(logicalHeight) / (float)fontSize;

    /// <summary>Calculates a local PixelSize for a label nested inside a scaled parent.</summary>
    public static float GetLocalPixelSize(float logicalHeight, float parentLogicalWidth, int fontSize = StandardFontSize)
        => (logicalHeight / parentLogicalWidth) / (float)fontSize;
    
    /// <summary>Converts a 2D logical position to a 3D world position on the XZ plane.</summary>
    public static Vector3 ToWorldPos(System.Numerics.Vector2 logicalPos, float yPos = 0f) 
        => new Vector3(logicalPos.X * LogicToWorldScale, yPos, logicalPos.Y * LogicToWorldScale);
    
    /// <summary>Converts a 3D world position to a 2D logical position.</summary>
    public static System.Numerics.Vector2 ToLogicalPos(Vector3 worldPos) 
        => new System.Numerics.Vector2(worldPos.X / LogicToWorldScale, worldPos.Z / LogicToWorldScale);
}
