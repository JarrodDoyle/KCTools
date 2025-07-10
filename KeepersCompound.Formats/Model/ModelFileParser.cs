using System.Drawing;
using System.Numerics;

namespace KeepersCompound.Formats.Model;

public class ModelFileParser : IBinaryParser<ModelFile>
{
    public ModelFile? Read(BinaryReader reader)
    {
        reader.BaseStream.Position = 0;
        if (reader.BaseStream.Length < 8)
        {
            return null;
        }

        var signature = reader.ReadNullString(4);
        var version = reader.ReadInt32();
        if (signature != "LGMD" || version < 3)
        {
            return null;
        }

        var name = reader.ReadNullString(8);
        var radius = reader.ReadSingle();
        var maxPolygonRadius = reader.ReadSingle();
        var maxBounds = reader.ReadVec3();
        var minBounds = reader.ReadVec3();
        var center = reader.ReadVec3();
        var polygonCount = reader.ReadUInt16();
        var vertexPositionCount = reader.ReadUInt16();
        var jointCount = reader.ReadUInt16();
        var materialCount = reader.ReadByte();
        var vCallCount = reader.ReadByte();
        var vHotCount = reader.ReadByte();
        var objectCount = reader.ReadByte();
        var objectOffset = reader.ReadUInt32();
        var materialOffset = reader.ReadUInt32();
        var vertexUvOffset = reader.ReadUInt32();
        var vHotOffset = reader.ReadUInt32();
        var vertexPositionOffset = reader.ReadUInt32();
        var vertexNormalOffset = reader.ReadUInt32();
        var faceNormalOffset = reader.ReadUInt32();
        var polygonOffset = reader.ReadUInt32();
        var nodeOffset = reader.ReadUInt32();
        var modelSize = reader.ReadUInt32();
        var auxMaterialFlags = version == 4 ? reader.ReadUInt32() : 0;
        var auxMaterialOffset = version == 4 ? reader.ReadUInt32() : 0;
        var auxMaterialSize = version == 4 ? reader.ReadUInt32() : 0;

        var transparency = (auxMaterialFlags & 0x1) != 0;
        var selfIllumination = (auxMaterialFlags & 0x2) != 0;

        var vertexPositions = new List<Vector3>(vertexPositionCount);
        reader.BaseStream.Seek(vertexPositionOffset, SeekOrigin.Begin);
        for (var i = 0; i < vertexPositionCount; i++)
        {
            vertexPositions.Add(reader.ReadVec3());
        }

        var vertexUvCount = (int)((vHotOffset - vertexUvOffset) / 8);
        var vertexUvs = new List<Vector2>(vertexUvCount);
        reader.BaseStream.Seek(vertexUvOffset, SeekOrigin.Begin);
        for (var i = 0; i < vertexUvCount; i++)
        {
            vertexUvs.Add(reader.ReadVec2());
        }

        var vertexNormalCount = (int)((faceNormalOffset - vertexNormalOffset) / 8);
        var vertexNormals = new List<ModelVertexNormal>(vertexNormalCount);
        reader.BaseStream.Seek(vertexNormalOffset, SeekOrigin.Begin);
        for (var i = 0; i < vertexNormalCount; i++)
        {
            var vnMaterialId = reader.ReadUInt16();
            var vnVertexId = reader.ReadUInt16();
            var vnCompactNormal = reader.ReadUInt32();
            var vnNormal = new Vector3(
                (short)((vnCompactNormal >> 16) & 0xFFC0) / 16384.0f,
                (short)((vnCompactNormal >> 6) & 0xFFC0) / 16384.0f,
                (short)((vnCompactNormal << 4) & 0xFFC0) / 16384.0f);
            vertexNormals.Add(new ModelVertexNormal
            {
                MaterialId = vnMaterialId,
                VertexId = vnVertexId,
                Normal = vnNormal,
            });
        }

        var faceNormalCount = (int)((polygonOffset - faceNormalOffset) / 12);
        var faceNormals = new List<Vector3>(faceNormalCount);
        reader.BaseStream.Seek(faceNormalOffset, SeekOrigin.Begin);
        for (var i = 0; i < faceNormalCount; i++)
        {
            faceNormals.Add(reader.ReadVec3());
        }

        var vhots = new List<ModelVHot>(vHotCount);
        reader.BaseStream.Seek(vHotOffset, SeekOrigin.Begin);
        for (var i = 0; i < vHotCount; i++)
        {
            vhots.Add(new ModelVHot
            {
                Type = (ModelVHotType)reader.ReadInt32(),
                Position = reader.ReadVec3(),
            });
        }

        var polygons = new List<ModelPolygon>(polygonCount);
        reader.BaseStream.Seek(polygonOffset, SeekOrigin.Begin);
        for (var i = 0; i < polygonCount; i++)
        {
            var polyIndex = reader.ReadUInt16();
            var polyData = reader.ReadUInt16();
            var polyRawType = reader.ReadByte();
            var polyType = (ModelPolygonType)(polyRawType & 0x07);
            var polyColorType = (ModelPolygonColorType)((polyRawType & 0x60) >> 5);
            var polyUseVertexNormals = (polyRawType & 0x18) != 0;
            var polyVertexCount = reader.ReadByte();
            var polyNormalIndex = reader.ReadUInt16();
            var polyNormalDistance = reader.ReadSingle();
            var polyVertexIndicesRaw = new ushort[3 * polyVertexCount];
            for (var j = 0; j < 3 * polyVertexCount; j++)
            {
                if (j >= 2 * polyVertexCount && polyType != ModelPolygonType.Textured)
                {
                    polyVertexIndicesRaw[j] = 0;
                    continue;
                }

                polyVertexIndicesRaw[j] = reader.ReadUInt16();
            }

            var polyVertexIndices = new List<ModelPolygonVertex>(polyVertexCount);
            for (var j = 0; j < polyVertexCount; j++)
            {
                polyVertexIndices.Add(new ModelPolygonVertex
                {
                    PositionIndex = polyVertexIndicesRaw[j],
                    NormalIndex = polyVertexIndicesRaw[j + polyVertexCount],
                    UvIndex = polyVertexIndicesRaw[j + 2 * polyVertexCount],
                });
            }

            var polyMaterialId = version == 4 ? reader.ReadByte() : (byte)0;

            polygons.Add(new ModelPolygon
            {
                Index = polyIndex,
                Type = polyType,
                ColorType = polyColorType,
                Data = polyData,
                UseVertexNormals = polyUseVertexNormals,
                NormalIndex = polyNormalIndex,
                NormalDistance = polyNormalDistance,
                VertexIndices = polyVertexIndices,
                MaterialId = polyMaterialId,
            });
        }

        var objects = new List<ModelObject>(objectCount);
        reader.BaseStream.Seek(objectOffset, SeekOrigin.Begin);
        for (var i = 0; i < objectCount; i++)
        {
            var objName = reader.ReadNullString(8);
            var objJointType = (ModelObjectType)reader.ReadByte();
            var objJointIndex = reader.ReadInt32();
            var objJointMinValue = reader.ReadSingle();
            var objJointMaxValue = reader.ReadSingle();
            var v1 = reader.ReadVec3();
            var v2 = reader.ReadVec3();
            var v3 = reader.ReadVec3();
            var v4 = reader.ReadVec3();
            var objTransform = new Matrix4x4(
                v1.X, v1.Y, v1.Z, 0,
                v2.X, v2.Y, v2.Z, 0,
                v3.X, v3.Y, v3.Z, 0,
                v4.X, v4.Y, v4.Z, 1);
            var objChild = reader.ReadInt16();
            var objNext = reader.ReadInt16();
            var objVhotStartIdx = reader.ReadUInt16();
            var objVhotCount = reader.ReadUInt16();
            var objVertexStartIdx = reader.ReadUInt16();
            var objVertexCount = reader.ReadUInt16();
            var objVertexNormalStartIdx = reader.ReadUInt16();
            var objVertexNormalCount = reader.ReadUInt16();
            var objFaceNormalStartIdx = reader.ReadUInt16();
            var objFaceNormalCount = reader.ReadUInt16();
            var objNodeStartIdx = reader.ReadUInt16();
            var objNodeCount = reader.ReadUInt16();

            objects.Add(new ModelObject
            {
                Name = objName,
                JointType = objJointType,
                JointIndex = objJointIndex,
                JointMinValue = objJointMinValue,
                JointMaxValue = objJointMaxValue,
                Transform = objTransform,
                ChildObjectIndex = objChild,
                SiblingObjectIndex = objNext,
                VHotStartIndex = objVhotStartIdx,
                VHotCount = objVhotCount,
                VertexPositionStartIndex = objVertexStartIdx,
                VertexPositionCount = objVertexCount,
                VertexNormalStartIndex = objVertexNormalStartIdx,
                VertexNormalCount = objVertexNormalCount,
                FaceNormalStartIndex = objFaceNormalStartIdx,
                FaceNormalCount = objFaceNormalCount,
                BspNodeStartIndex = objNodeStartIdx,
                BspNodeCount = objNodeCount,
            });
        }

        var materials = new List<ModelMaterial>(materialCount);
        reader.BaseStream.Seek(materialOffset, SeekOrigin.Begin);
        for (var i = 0; i < materialCount; i++)
        {
            var matName = reader.ReadNullString(16);
            var matType = (ModelMaterialType)reader.ReadByte();
            var matSlot = reader.ReadByte();
            var matColor = Color.FromArgb(reader.ReadInt32());
            var matPaletteIndex = reader.ReadUInt32();

            materials.Add(new ModelMaterial
            {
                Name = matName,
                Type = matType,
                Slot = matSlot,
                Color = matColor,
                PaletteIndex = matPaletteIndex,
                Transparency = 0,
                SelfIllumination = 0,
                MaxTexelSize = default,
            });
        }

        if (version == 4)
        {
            reader.BaseStream.Seek(auxMaterialOffset, SeekOrigin.Begin);
            for (var i = 0; i < materialCount; i++)
            {
                materials[i].Transparency = reader.ReadSingle();
                materials[i].SelfIllumination = reader.ReadSingle();
                if (auxMaterialSize == 16)
                {
                    materials[i].MaxTexelSize = reader.ReadVec2();
                }
            }
        }

        reader.BaseStream.Seek(vertexNormalOffset, SeekOrigin.Begin);
        var bspNodeData = reader.ReadBytes((int)(modelSize - nodeOffset)).ToList();

        return new ModelFile
        {
            Version = version,
            Name = name,
            Radius = radius,
            MaxPolygonRadius = maxPolygonRadius,
            MinBounds = minBounds,
            MaxBounds = maxBounds,
            Center = center,
            Transparency = transparency,
            SelfIllumination = selfIllumination,
            JointCount = jointCount,
            VCallCount = vCallCount,
            VertexPositions = vertexPositions,
            VertexUvs = vertexUvs,
            VertexNormals = vertexNormals,
            FaceNormals = faceNormals,
            Polygons = polygons,
            VHots = vhots,
            Materials = materials,
            Objects = objects,
            BspNodeData = bspNodeData
        };
    }

    public void Write(BinaryWriter writer)
    {
        throw new NotImplementedException();
    }
}