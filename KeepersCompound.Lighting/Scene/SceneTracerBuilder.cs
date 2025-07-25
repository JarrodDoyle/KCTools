using System.Numerics;
using KeepersCompound.Dark.Database;
using KeepersCompound.Dark.Database.Chunks;
using KeepersCompound.Dark.Resources;
using Serilog;
using TinyEmbree;

namespace KeepersCompound.Lighting.Scene;

public static class SceneTracerBuilder
{
    private const int SkyHack = 249;

    public static SceneTracer Build(
        WorldRep worldRep,
        BrList brushList,
        ObjectHierarchy hierarchy,
        ResourceManager resources)
    {
        var polyVertices = new List<Vector3>();
        var vertices = new List<Vector3>();
        var indices = new List<int>();
        var surfaceMap = new List<SceneSurfaceType>();

        foreach (var cell in worldRep.Cells)
        {
            // We only care about polys representing solid terrain. We can't use RenderPolyCount because that includes
            // water surfaces.
            var solidPolys = cell.PolyCount - cell.PortalPolyCount;
            var cellIdxOffset = 0;
            for (var polyIdx = 0; polyIdx < solidPolys; polyIdx++)
            {
                var poly = cell.Polys[polyIdx];
                polyVertices.Clear();
                polyVertices.EnsureCapacity(poly.VertexCount);
                for (var i = 0; i < poly.VertexCount; i++)
                {
                    polyVertices.Add(cell.Vertices[cell.Indices[cellIdxOffset + i]]);
                }

                var primType = cell.RenderPolys[polyIdx].TextureId == SkyHack
                    ? SceneSurfaceType.Sky
                    : SceneSurfaceType.Terrain;
                AddPolygon(vertices, indices, surfaceMap, polyVertices, primType);
                cellIdxOffset += poly.VertexCount;
            }
        }

        var terrainTracer = new Raytracer();
        terrainTracer.AddMesh(new TriangleMesh([..vertices], [..indices]));
        terrainTracer.CommitScene();

        foreach (var brush in brushList.Brushes)
        {
            if (brush.Media != Media.Object)
            {
                continue;
            }

            var id = (int)brush.BrushInfo;
            var modelNameProp = hierarchy.GetProperty<PropLabel>(id, "P$ModelName");
            var scaleProp = hierarchy.GetProperty<PropVector>(id, "P$Scale");
            var renderTypeProp = hierarchy.GetProperty<PropRenderType>(id, "P$RenderTyp");
            var jointPosProp = hierarchy.GetProperty<PropJointPos>(id, "P$JointPos");
            var immobileProp = hierarchy.GetProperty<PropBool>(id, "P$Immobile");
            var staticShadowProp = hierarchy.GetProperty<PropBool>(id, "P$StatShad");

            var joints = jointPosProp?.Positions ?? [0, 0, 0, 0, 0, 0];
            var castsShadows = (immobileProp?.Value ?? false) || (staticShadowProp?.Value ?? false);
            var renderMode = renderTypeProp?.RenderMode ?? RenderMode.Normal;
            if (modelNameProp == null || !castsShadows || renderMode == RenderMode.CoronaOnly)
            {
                continue;
            }

            if (!resources.TryGetModel(modelNameProp.Value, out var model))
            {
                Log.Warning("Failed to find model file: {Name}", modelNameProp.Value);
                continue;
            }

            // Calculate base model transform
            var baseTransform = Matrix4x4.CreateScale(scaleProp?.Value ?? Vector3.One);
            baseTransform *= Matrix4x4.CreateRotationX(float.DegreesToRadians(brush.Angle.X));
            baseTransform *= Matrix4x4.CreateRotationY(float.DegreesToRadians(brush.Angle.Y));
            baseTransform *= Matrix4x4.CreateRotationZ(float.DegreesToRadians(brush.Angle.Z));
            baseTransform *= Matrix4x4.CreateTranslation(brush.Position - model.Center);

            // For each polygon slam its vertices and indices :)
            var objTransforms = model.GetObjectTransforms(baseTransform, [..joints]);
            foreach (var poly in model.Polygons)
            {
                var objId = model.GetPolygonObjectMapping(poly);
                if (objId == -1)
                {
                    continue;
                }

                var transform = objTransforms[objId];
                polyVertices.Clear();
                polyVertices.EnsureCapacity(poly.VertexIndices.Count);
                foreach (var idx in poly.VertexIndices)
                {
                    var vertex = model.VertexPositions[idx.PositionIndex];
                    vertex = Vector3.Transform(vertex, transform);
                    polyVertices.Add(vertex);
                }

                AddPolygon(vertices, indices, surfaceMap, polyVertices, SceneSurfaceType.Object);
            }
        }

        var terrainObjTracer = new Raytracer();
        terrainObjTracer.AddMesh(new TriangleMesh([..vertices], [..indices]));
        terrainObjTracer.CommitScene();

        return new SceneTracer(terrainTracer, terrainObjTracer, surfaceMap);
    }

    private static void AddPolygon(
        List<Vector3> vertices,
        List<int> indices,
        List<SceneSurfaceType> surfaceMap,
        List<Vector3> polyVertices,
        SceneSurfaceType polySurfaceType)
    {
        var vertexCount = polyVertices.Count;
        var indexOffset = vertices.Count;

        // Polygons are n-sided, but fortunately they're convex so we can just do a fan triangulation
        // Embree triangle winding order is reverse of LGS winding order, so we go (0, i+1, i) instead of (0, i, i+1)
        vertices.AddRange(polyVertices);
        for (var i = 1; i < vertexCount - 1; i++)
        {
            indices.Add(indexOffset);
            indices.Add(indexOffset + i + 1);
            indices.Add(indexOffset + i);
            surfaceMap.Add(polySurfaceType);
        }
    }
}