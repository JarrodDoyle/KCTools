using System.Numerics;

namespace KeepersCompound.Dark.Portalisation.Brush;

public struct BrushDefFace
{
    public Plane Plane;
    public int TextureId;
    public Vector3 UProjection;
    public Vector3 VProjection;
    public float UScale;
    public float VScale;
    public float Rotation;
    public Vector2 Offset;

    public BrushDefFace(Plane plane, int textureId, Vector3 uProjection, Vector3 vProjection, float uScale, float vScale,
        float rotation, Vector2 offset)
    {
        Plane = Plane.Normalize(plane);
        TextureId = textureId;
        UProjection = uProjection;
        VProjection = vProjection;
        UScale = uScale;
        VScale = vScale;
        Rotation = rotation;
        Offset = offset;
    }
}