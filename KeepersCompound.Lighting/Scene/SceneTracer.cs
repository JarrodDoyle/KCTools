using System.Numerics;
using TinyEmbree;

namespace KeepersCompound.Lighting.Scene;

public class SceneTracer
{
    private const float Epsilon = MathUtils.Epsilon;

    private readonly Raytracer _terrainTracer;
    private readonly Raytracer _terrainObjTracer;
    private readonly List<SceneSurfaceType> _surfaceMap;

    public SceneTracer(Raytracer terrainTracer, Raytracer terrainObjTracer, List<SceneSurfaceType> surfaceMap)
    {
        _terrainTracer = terrainTracer;
        _terrainObjTracer = terrainObjTracer;
        _surfaceMap = surfaceMap;
    }

    public Hit Trace(SceneTracerType tracerType, Vector3 origin, Vector3 direction)
    {
        var ray = new Ray
        {
            Origin = origin,
            Direction = Vector3.Normalize(direction)
        };

        return tracerType switch
        {
            SceneTracerType.Terrain => _terrainTracer.Trace(ray),
            SceneTracerType.TerrainAndObjects => _terrainObjTracer.Trace(ray),
            _ => throw new ArgumentOutOfRangeException(nameof(tracerType), tracerType, null)
        };
    }

    public bool TraceOcclusion(SceneTracerType tracerType, Vector3 origin, Vector3 target)
    {
        var direction = target - origin;
        var ray = new Ray
        {
            Origin = origin,
            Direction = Vector3.Normalize(direction)
        };
        var shadowRay = new ShadowRay(ray, direction.Length() - Epsilon);

        return tracerType switch
        {
            SceneTracerType.Terrain => _terrainTracer.IsOccluded(shadowRay),
            SceneTracerType.TerrainAndObjects => _terrainObjTracer.IsOccluded(shadowRay),
            _ => throw new ArgumentOutOfRangeException(nameof(tracerType), tracerType, null)
        };
    }

    public bool TraceSun(Vector3 origin, Vector3 direction)
    {
        var hit = Trace(SceneTracerType.TerrainAndObjects, origin, direction);

        // If origin is very close to a wall, the initial trace to the sun sometimes misses the wall. Now that we have
        // backface culling enabled in Embree, this can result in reaching a sky when we shouldn't.
        // By doing another occlusion trace in the reverse direction we fix this. Any backfaces we passed through in
        // the initial trace become frontfaces to be occluded by.
        if (hit && !TraceOcclusion(SceneTracerType.TerrainAndObjects,
                hit.Position + hit.ErrorOffset * hit.Normal, origin))
        {
            return _surfaceMap[(int)hit.PrimId] == SceneSurfaceType.Sky;
        }

        return false;
    }
}