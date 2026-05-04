using System.Numerics;

namespace KeepersCompound.Dark.Portalisation.Brush;

public class BrushTexInfo
{
    public uint TextureId { get; }
    public Vector3 UProjection { get; }
    public Vector3 VProjection { get; }
    public float UScale { get; }
    public float VScale { get; }
    public float Rotation { get; }
    public Vector2 Offset { get; }

    public BrushTexInfo(
        uint textureId,
        Vector3 uProjection,
        Vector3 vProjection,
        float uScale,
        float vScale,
        float rotation,
        Vector2 offset)
    {
        TextureId = textureId;
        UProjection = uProjection;
        VProjection = vProjection;
        UScale = uScale;
        VScale = vScale;
        Rotation = rotation;
        Offset = offset;
    }
}