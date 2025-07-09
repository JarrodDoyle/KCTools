using System.Numerics;

namespace KeepersCompound.Formats.Model;

public class ModelObject
{
    /// <summary>
    /// Maximum 8 characters. Additional characters will be truncated on write.
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Joint movement type.
    /// </summary>
    public required ModelObjectType JointType { get; set; }
    
    /// <summary>
    /// Which joint parameter to apply.
    /// </summary>
    public required int JointIndex { get; set; }
    
    /// <summary>
    /// Minimum allowable value for the joint parameter.
    /// </summary>
    public required float JointMinValue { get; set; }
    
    /// <summary>
    /// Maximum allowable value for the joint parameter.
    /// </summary>
    public required float JointMaxValue { get; set; }
    
    /// <summary>
    /// Base transform to apply to object vertices and vhots
    /// </summary>
    public required Matrix4x4 Transform { get; set; }
    
    /// <summary>
    /// Index of the first child object. -1 if no children.
    /// </summary>
    public required short ChildObjectIndex { get; set; }
    
    /// <summary>
    /// Index of the next sibling object. -1 if no more siblings.
    /// </summary>
    public required short SiblingObjectIndex { get; set; }
    
    /// <summary>
    /// Index into <see cref="ModelFile.VHots"/> of the first VHot used by this object.
    /// </summary>
    public required ushort VHotStartIndex { get; set; }
    
    /// <summary>
    /// Number of VHots used by this object.
    /// </summary>
    public required ushort VHotCount { get; set; }
    
    /// <summary>
    /// Index into <see cref="ModelFile.VertexPositions"/> of the first vertex position used by this object.
    /// </summary>
    public required ushort VertexPositionStartIndex { get; set; }
    
    /// <summary>
    /// Number of vertex positions used by this object.
    /// </summary>
    public required ushort VertexPositionCount { get; set; }
    
    /// <summary>
    /// Index into <see cref="ModelFile.VertexNormals"/> of the first vertex normal used by this object.
    /// </summary>
    public required ushort VertexNormalStartIndex { get; set; }
    
    /// <summary>
    /// Number of vertex normals used by this object.
    /// </summary>
    public required ushort VertexNormalCount { get; set; }
    
    /// <summary>
    /// Index into <see cref="ModelFile.FaceNormals"/> of the first face normal used by this object.
    /// </summary>
    public required ushort FaceNormalStartIndex { get; set; }
    
    /// <summary>
    /// Number of face normals used by this object.
    /// </summary>
    public required ushort FaceNormalCount { get; set; }
    
    /// <summary>
    /// Index into <see cref="ModelFile.BspNodes"/> of the first BSP node used by this object.
    /// </summary>
    public required ushort BspNodeStartIndex { get; set; }
    
    /// <summary>
    /// Number of BSP nodes used by this object.
    /// </summary>
    public required ushort BspNodeCount { get; set; }
}