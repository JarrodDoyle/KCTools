using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Serilog;

namespace KeepersCompound.Formats.Model;

/// <summary>
/// Structure representing a Dark Engine .BIN model file. Read and write using a <see cref="ModelFileParser"/>.
/// </summary>
public class ModelFile
{
    /// <summary>
    /// Version of BSP used to generate model. Supported versions are 3 and 4.
    /// </summary>
    public required int Version { get; set; }

    /// <summary>
    /// Maximum 8 characters. Additional characters will be truncated on write.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Model bounding sphere radius centered on (0, 0, 0).
    /// </summary>
    public required float Radius { get; set; }

    /// <summary>
    /// Maximum bounding sphere radius of the models polygons.
    /// </summary>
    public required float MaxPolygonRadius { get; set; }

    /// <summary>
    /// Minimum point of the models AABB
    /// </summary>
    public required Vector3 MinBounds { get; set; }

    /// <summary>
    /// Maximum point of the models AABB
    /// </summary>
    public required Vector3 MaxBounds { get; set; }

    /// <summary>
    /// Model center offset
    /// </summary>
    public required Vector3 Center { get; set; }

    /// <summary>
    /// Uses transparency on one or more materials. Only supported on version 4.
    /// </summary>
    public required bool Transparency { get; set; }

    /// <summary>
    /// Uses self-illumination on one or more materials. Only supported on version 4.
    /// </summary>
    public required bool SelfIllumination { get; set; }

    /// <summary>
    /// How large is the auxillary material data. Only supported on version 4.
    /// </summary>
    public required uint AuxMaterialSize { get; set; }

    /// <summary>
    /// Number of joints/parameters. DromEd can only interact with the first 6.
    /// </summary>
    public required ushort JointCount { get; set; }

    /// <summary>
    /// Number of VCalls in the BSP tree
    /// </summary>
    public required byte VCallCount { get; set; }

    /// <summary>
    /// Global vertex position list
    /// </summary>
    public required List<Vector3> VertexPositions { get; set; }

    /// <summary>
    /// Global vertex UV list
    /// </summary>
    public required List<Vector2> VertexUvs { get; set; }

    /// <summary>
    /// Global vertex normal list
    /// </summary>
    public required List<ModelVertexNormal> VertexNormals { get; set; }

    /// <summary>
    /// Global face normal list
    /// </summary>
    public required List<Vector3> FaceNormals { get; set; }

    /// <summary>
    /// Global polygon list
    /// </summary>
    public required List<ModelPolygon> Polygons { get; set; }

    /// <summary>
    /// Global vhot list. Each vhot must have a unique <see cref="ModelVHotType"/>.
    /// </summary>
    public required List<ModelVHot> VHots { get; set; }

    public required List<ModelMaterial> Materials { get; set; }

    public required List<ModelObject> Objects { get; set; }

    /// <summary>
    /// Unknown structure at this point.
    /// </summary>
    public required List<byte> BspNodeData { get; set; }

    /// <summary>
    /// Get the transforms to apply to each <see cref="ModelObject"/> after applying joint values.
    /// </summary>
    /// <param name="baseTransform">Base transform of the model.</param>
    /// <param name="jointValues">Values for each joint. Note that DromEd properties only expose 6 joints, but there can be many more.</param>
    /// <returns></returns>
    public List<Matrix4x4> GetObjectTransforms(Matrix4x4 baseTransform, List<float> jointValues)
    {
        // Build map of objects to their parent id
        var objCount = Objects.Count;
        var parentIds = new int[objCount];
        for (var i = 0; i < objCount; i++)
        {
            parentIds[i] = -1;
        }

        for (var i = 0; i < objCount; i++)
        {
            var subObj = Objects[i];
            var childIdx = subObj.ChildObjectIndex;
            while (childIdx != -1)
            {
                // This can only happen if there's a loop in the relationship. This shouldn't ever be the case, but for
                // some reason a few Thief 2 objects have this.
                if (childIdx == i)
                {
                    break;
                }

                parentIds[childIdx] = i;
                childIdx = Objects[childIdx].SiblingObjectIndex;
            }
        }

        // Calculate base transforms for every subobj (including joint)
        var subObjTransforms = new Matrix4x4[objCount];
        for (var i = 0; i < objCount; i++)
        {
            var subObj = Objects[i];
            var objTrans = Matrix4x4.Identity;

            if (subObj.JointType == ModelObjectType.Rotating && subObj.JointIndex != -1)
            {
                var ang = subObj.JointIndex >= jointValues.Count ? 0 : float.DegreesToRadians(jointValues[subObj.JointIndex]);
                var jointRot = Matrix4x4.CreateFromYawPitchRoll(0, ang, 0);
                objTrans = jointRot * subObj.Transform;
            }
            else if (subObj.JointType == ModelObjectType.Sliding && subObj.JointIndex != -1)
            {
                var dist = subObj.JointIndex >= jointValues.Count ? 0 : jointValues[subObj.JointIndex];
                var translation = Matrix4x4.CreateTranslation(dist, 0, 0);
                objTrans = translation * subObj.Transform;
            }

            subObjTransforms[i] = objTrans;
        }

        // Final transforms are composed by climbing the hierarchy and applying parent transforms
        var transforms = new List<Matrix4x4>(objCount);
        for (var i = 0; i < objCount; i++)
        {
            var transform = subObjTransforms[i];

            // Build compound transformation
            var parentId = parentIds[i];
            while (parentId != -1)
            {
                transform *= subObjTransforms[parentId];
                parentId = parentIds[parentId];
            }

            transform *= baseTransform;
            transforms.Add(transform);
        }

        return transforms;
    }

    /// <summary>
    /// Attempt to get a vhot of a given type.
    /// </summary>
    /// <param name="type">Which vhot type is being requested</param>
    /// <param name="vhot">The found vhot. Null if not found.</param>
    /// <returns>True if the vhot was found, false otherwise.</returns>
    public bool TryGetVhot(ModelVHotType type, [MaybeNullWhen(false)] out ModelVHot vhot)
    {
        foreach (var v in VHots)
        {
            if (v.Type == type)
            {
                vhot = v;
                return true;
            }
        }

        vhot = null;
        return false;
    }

    /// <summary>
    /// Determine which <see cref="ModelObject"/> a <see cref="ModelPolygon"/> belongs to.
    /// </summary>
    /// <param name="polygon">Target polygon.</param>
    /// <returns>The index of the owning <see cref="ModelObject"/> in <see cref="Objects"/> if one exists otherwise -1.</returns>
    public int GetPolygonObjectMapping(ModelPolygon polygon)
    {
        // The simplest way to detect which object a polygon belongs to is to just check if a vertex position index of
        // the polygon is within the objects vertex position range.
        var vertexIndex = polygon.VertexIndices[0].PositionIndex;
        for (var i = 0; i < Objects.Count; i++)
        {
            var obj = Objects[i];
            var start = (int)obj.VertexPositionStartIndex;
            var end = start + obj.VertexPositionCount;
            if (start <= vertexIndex && vertexIndex < end)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Determine which <see cref="ModelObject"/> a <see cref="ModelVHot"/> belongs to.
    /// </summary>
    /// <param name="type">Target vhot.</param>
    /// <returns>The index of the owning <see cref="ModelObject"/> in <see cref="Objects"/> if one exists otherwise -1.</returns>
    public int GetVhotObjectMapping(ModelVHotType type)
    {
        for (var i = 0; i < Objects.Count; i++)
        {
            var obj = Objects[i];
            for (var j = 0; j < obj.VHotCount; j++)
            {
                var vhot = VHots[obj.VHotStartIndex + j];
                if (vhot.Type == type)
                {
                    return i;
                }
            }
        }

        return -1;
    }
}