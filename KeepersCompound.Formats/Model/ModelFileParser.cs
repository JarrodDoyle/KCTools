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

            // Just because the model says it has a certain amount of polygons, doesn't mean it actually does :))
            if (reader.BaseStream.Position >= nodeOffset)
            {
                break;
            }
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

        reader.BaseStream.Seek(nodeOffset, SeekOrigin.Begin);
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
            AuxMaterialSize = auxMaterialSize,
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

    public void Write(BinaryWriter writer, ModelFile modelFile)
    {
        writer.WriteNullString("LGMD", 4);
        writer.Write(modelFile.Version);
        writer.WriteNullString(modelFile.Name, 8);
        writer.Write(modelFile.Radius);
        writer.Write(modelFile.MaxPolygonRadius);
        writer.WriteVec3(modelFile.MaxBounds);
        writer.WriteVec3(modelFile.MinBounds);
        writer.WriteVec3(modelFile.Center);

        var maxPolyIndex = 0;
        foreach (var polygon in modelFile.Polygons)
        {
            if (polygon.Index > maxPolyIndex)
            {
                maxPolyIndex = polygon.Index;
            }
        }

        writer.Write((ushort)(maxPolyIndex + 1));
        writer.Write((ushort)modelFile.VertexPositions.Count);
        writer.Write(modelFile.JointCount);
        writer.Write((byte)modelFile.Materials.Count);
        writer.Write(modelFile.VCallCount);
        writer.Write((byte)modelFile.VHots.Count);
        writer.Write((byte)modelFile.Objects.Count);

        var offsetsOffset = writer.BaseStream.Position;
        writer.Write(new byte[40]);

        if (modelFile.Version == 4)
        {
            writer.Write((modelFile.Transparency ? 0x1 : 0) + (modelFile.SelfIllumination ? 0x2 : 0));
            writer.Write(new byte[4]);
            writer.Write(modelFile.AuxMaterialSize);
        }

        var objectsOffset = writer.BaseStream.Position;
        foreach (var obj in modelFile.Objects)
        {
            writer.WriteNullString(obj.Name, 8);
            writer.Write((byte)obj.JointType);
            writer.Write(obj.JointIndex);
            writer.Write(obj.JointMinValue);
            writer.Write(obj.JointMaxValue);
            writer.Write(obj.Transform.M11);
            writer.Write(obj.Transform.M12);
            writer.Write(obj.Transform.M13);
            writer.Write(obj.Transform.M21);
            writer.Write(obj.Transform.M22);
            writer.Write(obj.Transform.M23);
            writer.Write(obj.Transform.M31);
            writer.Write(obj.Transform.M32);
            writer.Write(obj.Transform.M33);
            writer.Write(obj.Transform.M41);
            writer.Write(obj.Transform.M42);
            writer.Write(obj.Transform.M43);
            writer.Write(obj.ChildObjectIndex);
            writer.Write(obj.SiblingObjectIndex);
            writer.Write(obj.VHotStartIndex);
            writer.Write(obj.VHotCount);
            writer.Write(obj.VertexPositionStartIndex);
            writer.Write(obj.VertexPositionCount);
            writer.Write(obj.VertexNormalStartIndex);
            writer.Write(obj.VertexNormalCount);
            writer.Write(obj.FaceNormalStartIndex);
            writer.Write(obj.FaceNormalCount);
            writer.Write(obj.BspNodeStartIndex);
            writer.Write(obj.BspNodeCount);
        }
        
        var materialsOffset = writer.BaseStream.Position;
        foreach (var material in modelFile.Materials)
        {
            writer.WriteNullString(material.Name, 16);
            writer.Write((byte)material.Type);
            writer.Write(material.Slot);
            writer.Write(material.Color.ToArgb());
            writer.Write(material.PaletteIndex);
        }
        
        var auxMaterialsOffset = writer.BaseStream.Position;
        if (modelFile.Version == 4)
        {
            foreach (var material in modelFile.Materials)
            {
                writer.Write(material.Transparency);
                writer.Write(material.SelfIllumination);
                if (modelFile.AuxMaterialSize == 16)
                {
                    writer.WriteVec2(material.MaxTexelSize);
                }
            }
        }
        
        var vertexUvsOffset = writer.BaseStream.Position;
        foreach (var vertexUv in modelFile.VertexUvs)
        {
            writer.WriteVec2(vertexUv);
        }
        
        var vhotsOffset = writer.BaseStream.Position;
        foreach (var vhot in modelFile.VHots)
        {
            writer.Write((int)vhot.Type);
            writer.WriteVec3(vhot.Position);
        }
        
        var vertexPositionsOffset = writer.BaseStream.Position;
        foreach (var vertexPosition in modelFile.VertexPositions)
        {
            writer.WriteVec3(vertexPosition);
        }
        
        var vertexNormalsOffset = writer.BaseStream.Position;
        foreach (var vertexNormal in modelFile.VertexNormals)
        {
            writer.Write(vertexNormal.MaterialId);
            writer.Write(vertexNormal.VertexId);
            writer.Write((uint)((ushort)((int)(vertexNormal.Normal.X * 256) << 6) << 16) +
                         (uint)((ushort)((int)(vertexNormal.Normal.Y * 256) << 6) << 6) +
                         (uint)((ushort)((int)(vertexNormal.Normal.Z * 256) << 6) >> 4));
        }
        
        var faceNormalsOffset = writer.BaseStream.Position;
        foreach (var faceNormal in modelFile.FaceNormals)
        {
            writer.WriteVec3(faceNormal);
        }
        
        var polygonsOffset = writer.BaseStream.Position;
        foreach (var polygon in modelFile.Polygons)
        {
            writer.Write(polygon.Index);
            writer.Write(polygon.Data);
            writer.Write((byte)((byte)polygon.Type + ((byte)polygon.ColorType << 5) + (polygon.UseVertexNormals ? 0x18 : 0)));
            writer.Write((byte)polygon.VertexIndices.Count);
            writer.Write(polygon.NormalIndex);
            writer.Write(polygon.NormalDistance);
            foreach (var vertexIndex in polygon.VertexIndices)
            {
                writer.Write(vertexIndex.PositionIndex);
            }
        
            foreach (var vertexIndex in polygon.VertexIndices)
            {
                writer.Write(vertexIndex.NormalIndex);
            }
        
            if (polygon.Type == ModelPolygonType.Textured)
            {
                foreach (var vertexIndex in polygon.VertexIndices)
                {
                    writer.Write(vertexIndex.UvIndex);
                }
            }
        
            if (modelFile.Version == 4)
            {
                writer.Write(polygon.MaterialId);
            }
        }
        
        var bspNodesOffset = writer.BaseStream.Position;
        writer.Write(modelFile.BspNodeData.ToArray());
        
        var modelFileSize = writer.BaseStream.Position;
        writer.BaseStream.Seek(offsetsOffset, SeekOrigin.Begin);
        writer.Write((int)objectsOffset);
        writer.Write((int)materialsOffset);
        writer.Write((int)vertexUvsOffset);
        writer.Write((int)vhotsOffset);
        writer.Write((int)vertexPositionsOffset);
        writer.Write((int)vertexNormalsOffset);
        writer.Write((int)faceNormalsOffset);
        writer.Write((int)polygonsOffset);
        writer.Write((int)bspNodesOffset);
        writer.Write((int)modelFileSize);
        writer.BaseStream.Seek(4, SeekOrigin.Current);
        writer.Write((int)auxMaterialsOffset);
    }
}