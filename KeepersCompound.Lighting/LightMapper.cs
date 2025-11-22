using System.Numerics;
using KeepersCompound.Dark;
using KeepersCompound.Dark.Database;
using KeepersCompound.Dark.Database.Chunks;
using KeepersCompound.Dark.Resources;
using KeepersCompound.Formats.Model;
using KeepersCompound.Lighting.Vis;
using KeepersCompound.Lighting.Scene;
using Serilog;

namespace KeepersCompound.Lighting;

public class LightMapper
{
    // The objcast element of sunlight is ignored, we just care if it's quadlit
    private struct SunSettings
    {
        public bool Enabled;
        public bool QuadLit;
        public Vector3 Direction;
        public Vector3 Color;
    }

    private struct Settings
    {
        public Vector3[] AmbientLight;
        public bool Hdr;
        public float Attenuation;
        public float Saturation;
        public SoftnessMode MultiSampling;
        public float MultiSamplingCenterWeight;
        public bool LightmappedWater;
        public SunSettings Sunlight;
        public uint AnimLightCutoff;

        public override string ToString()
        {
            return $"Ambient Levels: {AmbientLight}, Hdr: {Hdr}, Attenuation: {Attenuation}, Saturation: {Saturation}";
        }
    }

    private ResourceManager _resources;
    private DbFile _mission;
    private ObjectHierarchy _hierarchy;
    private List<Light> _lights;
    private SceneTracer _scene;

    public LightMapper(ResourceManager resources, DbFile mission)
    {
        _resources = resources;
        _mission = mission;
        _hierarchy = Timing.TimeStage("Build Object Hierarchy", BuildHierarchy);
        _lights = [];

        VerifyRequiredChunksExist();

        if (!_mission.TryGetChunk<BrList>("BRLIST", out var brList) ||
            !_mission.TryGetChunk<WorldRep>("WREXT", out var worldRep))
        {
            return;
        }

        _scene = Timing.TimeStage("Build tracing scenes", () => SceneTracerBuilder.Build(worldRep, brList, _hierarchy, _resources));
    }

    public void Light()
    {
        // TODO: Throw?
        if (!_mission.TryGetChunk<RendParams>("RENDPARAMS", out var rendParams) ||
            !_mission.TryGetChunk<LmParams>("LM_PARAM", out var lmParams) ||
            !_mission.TryGetChunk<WorldRep>("WREXT", out var worldRep))
        {
            return;
        }

        var sunlightSettings = new SunSettings
        {
            Enabled = rendParams.UseSunlight,
            QuadLit = rendParams.SunlightMode is SunlightMode.QuadUnshadowed or SunlightMode.QuadObjcastShadows,
            Direction = Vector3.Normalize(rendParams.SunlightDirection),
            Color = Utils.HsbToRgb(rendParams.SunlightHue, rendParams.SunlightSaturation * lmParams.Saturation,
                rendParams.SunlightBrightness)
        };

        var ambientLight = rendParams.AmbientLightZones.ToList();
        ambientLight.Insert(0, rendParams.AmbientLight);
        for (var i = 0; i < ambientLight.Count; i++)
        {
            ambientLight[i] *= 255;
        }

        // TODO: lmParams LightmappedWater doesn't mean the game will actually *use* the lightmapped water hmm
        var lmFormat = worldRep.DataHeader.LightmapFormat;
        var settings = new Settings
        {
            Hdr = lmFormat == 2,
            AmbientLight = [..ambientLight],
            Attenuation = lmFormat == 0 ? 1.0f : lmParams.Attenuation,
            Saturation = lmFormat == 0 ? 1.0f : lmParams.Saturation,
            MultiSampling = lmParams.ShadowSoftness,
            MultiSamplingCenterWeight = lmParams.CenterWeight,
            LightmappedWater = lmParams.LightmappedWater,
            Sunlight = sunlightSettings,
            AnimLightCutoff = lmParams.AnimLightCutoff,
        };

        if (settings.AnimLightCutoff > 0)
        {
            Log.Warning(
                "Non-zero anim_light_cutoff ({Cutoff}). AnimLight lightmap shadow radius may not match lightgem shadow radius.",
                settings.AnimLightCutoff);
        }

        Timing.TimeStage("Set Door Blocking Flags", SetCellBlockingFlags);
        Timing.TimeStage("Gather Lights", () => BuildLightList(settings));
        Timing.TimeStage("Validate Lights", () => ValidateLightConfigurations(settings));
        Timing.TimeStage("Build Lighting Table", BuildLightingTable);
        Timing.TimeStage("Set Light Visibility", () => SetCellLightIndices(settings));
        Timing.TimeStage("Trace Scene", () => TraceScene(settings));
        Timing.TimeStage("Update AnimLight Cell Mapping", SetAnimLightCellMaps);
        Timing.TimeStage("Post-Light Validation", PostLightValidation);

        // We always do object casting, so it's nice to let dromed know that :)
        lmParams.ShadowType = LmParams.LightingMode.Objcast;
        if (rendParams is { UseSunlight: true, SunlightMode: SunlightMode.SingleUnshadowed })
        {
            rendParams.SunlightMode = SunlightMode.SingleObjcastShadows;
        }
        else if (rendParams is { UseSunlight: true, SunlightMode: SunlightMode.QuadUnshadowed })
        {
            rendParams.SunlightMode = SunlightMode.QuadObjcastShadows;
        }
    }

    public void Inspect()
    {
        if (!_mission.TryGetChunk<LmParams>("LM_PARAM", out var lmParams))
        {
            return;
        }

        if (lmParams.AnimLightCutoff > 0)
        {
            Log.Warning(
                "Non-zero anim_light_cutoff ({Cutoff}). AnimLight lightmap shadow radius may not match lightgem shadow radius.",
                lmParams.AnimLightCutoff);
        }

        var settings = new Settings();
        Timing.TimeStage("Gather Lights", () => BuildLightList(settings));
        Timing.TimeStage("Validate Lights", () => ValidateLightConfigurations(settings));
    }

    private bool VerifyRequiredChunksExist()
    {
        var requiredChunkNames = new[]
        {
            "RENDPARAMS",
            "LM_PARAM",
            "WREXT",
            "BRLIST",
            "P$AnimLight"
        };

        var allFound = true;
        foreach (var name in requiredChunkNames)
        {
            if (!_mission.Chunks.ContainsKey(name))
            {
                Log.Warning("Failed to find required chunk: {ChunkName}", name);
                allFound = false;
            }
        }

        return allFound;
    }

    private ObjectHierarchy BuildHierarchy()
    {
        if (!_mission.TryGetChunk<GamFile>("GAM_FILE", out var gamFile))
        {
            return new ObjectHierarchy(_mission);
        }

        if (_resources.TryGetDbFile(gamFile.FileName, out var gamesys))
        {
            return new ObjectHierarchy(_mission, gamesys);
        }

        Log.Warning("Failed to find GameSys");
        return new ObjectHierarchy(_mission);
    }

    /// <summary>
    /// Ensures "blocks_vision" and "can_block_vision" are set on cells containing doors.
    /// </summary>
    private void SetCellBlockingFlags()
    {
        if (!_mission.TryGetChunk<WorldRep>("WREXT", out var worldRep))
        {
            return;
        }

        var doorPositions = new List<Vector3>();
        var rotDoors = _hierarchy.GetConcreteObjectsWithPropDirect("P$RotDoor");
        foreach (var id in rotDoors)
        {
            var posProp = _hierarchy.GetProperty<PropPosition>(id, "P$Position", false);
            var doorProp = _hierarchy.GetProperty<PropRotDoor>(id, "P$RotDoor", false);
            if (posProp != null && doorProp is { BlocksVision: true, Status: DoorStatus.Closed })
            {
                doorPositions.Add(posProp.Location);
                Log.Debug("Found closed rotating door: {id}", id);
            }
        }

        var transDoors = _hierarchy.GetConcreteObjectsWithPropDirect("P$TransDoor");
        foreach (var id in transDoors)
        {
            var posProp = _hierarchy.GetProperty<PropPosition>(id, "P$Position", false);
            var doorProp = _hierarchy.GetProperty<PropTransDoor>(id, "P$TransDoor", false);
            if (posProp != null && doorProp is { BlocksVision: true, Status: DoorStatus.Closed })
            {
                doorPositions.Add(posProp.Location);
                Log.Debug("Found closed translating door: {id}", id);
            }
        }

        var cellCount = worldRep.Cells.Length;
        Parallel.For(0, doorPositions.Count, i =>
        {
            var pos = doorPositions[i];
            for (var j = 0; j < cellCount; j++)
            {
                var cell = worldRep.Cells[j];
                var contained = true;
                for (var k = 0; k < cell.PlaneCount; k++)
                {
                    var plane = cell.Planes[k];
                    if (MathUtils.DistanceFromPlane(plane, pos) < -MathUtils.Epsilon)
                    {
                        contained = false;
                        break;
                    }
                }

                if (contained)
                {
                    cell.Flags |= 16;
                    cell.Flags |= 8;
                    worldRep.Cells[j] = cell;
                    Log.Debug("Updated vision blocking flags for cell: {j}", j);
                }
            }
        });
    }

    private void BuildLightList(Settings settings)
    {
        _lights.Clear();

        if (!_mission.TryGetChunk<BrList>("BRLIST", out var brList))
        {
            return;
        }

        // TODO: Calculate the actual effective radius of infinite lights
        // potentially do the same for all lights and lower their radius if necessary?
        foreach (var brush in brList.Brushes)
        {
            switch (brush.Media)
            {
                case Media.Light:
                    ProcessBrushLight(brush, settings);
                    break;
                case Media.Object:
                    ProcessObjectLight(brush, settings);
                    break;
            }
        }
    }

    private void BuildLightingTable()
    {
        if (!_mission.TryGetChunk<WorldRep>("WREXT", out var worldRep))
        {
            return;
        }
        
        worldRep.LightingTable.Reset();
        foreach (var light in _lights)
        {
            // TODO: Set brush light index
            light.LightTableIndex = worldRep.LightingTable.LightCount;

            if (light.Anim)
            {
                var propAnimLight = _hierarchy.GetProperty<PropAnimLight>(light.ObjId, "P$AnimLight", false);
                propAnimLight!.LightTableLightIndex = (ushort)light.LightTableIndex;
            }

            worldRep.LightingTable.AddLight(light.ToLightData(32.0f));
        }
    }

    // TODO: Validate in-world here? Set cell idx on lights maybe?
    private void ValidateLightConfigurations(Settings settings)
    {
        var infinite = 0;
        for (var i = _lights.Count - 1; i > 0; i--)
        {
            var light = _lights[i];

            if (light.QuadLit && settings.MultiSampling != SoftnessMode.Standard)
            {
                if (light.ObjId != -1)
                {
                    Log.Warning(
                        "Object {Id}: Light flagged QuadLit but using Shadow Softness in build dialog. Shadow Softness overrides QuadLit.",
                        light.ObjId);
                }
                else
                {
                    Log.Warning(
                        "Brush at {Id}: Light flagged QuadLit but using Shadow Softness in build dialog. Shadow Softness overrides QuadLit.",
                        light.Position);
                }
            }

            if (light.Brightness == 0)
            {
                if (light.ObjId != -1)
                {
                    Log.Warning(
                        "Object {Id}: Zero brightness static light. Adjust brightness or remove un-used Light property.",
                        light.ObjId);
                }
                else
                {
                    Log.Warning("Brush at {Id}: Zero brightness static light. Adjust brightness or remove light.",
                        light.Position);
                }

                _lights.RemoveAt(i);
            }

            if (light.Hue < 0 || light.Hue > 1.0)
            {
                if (light.ObjId != -1)
                {
                    Log.Warning("Object {Id}: Hue {Hue} outside of range <0-1>.", light.ObjId, light.Hue);
                }
                else
                {
                    Log.Warning("Brush at {Position}: Hue {Hue} outside of range <0-1>.", light.Position, light.Hue);
                }
            }

            if (light.Saturation < 0 || light.Saturation > 1.0)
            {
                if (light.ObjId != -1)
                {
                    Log.Warning("Object {Id}: Saturation {Saturation} outside of range <0-1>.", light.ObjId, light.Saturation);
                }
                else
                {
                    Log.Warning("Brush at {Position}: Saturation {Saturation} outside of range <0-1>.", light.Position, light.Saturation);
                }
            }

            if (light.Radius == float.MaxValue)
            {
                if (light.ObjId != -1)
                {
                    Log.Warning("Object {Id}: Infinite light radius.", light.ObjId);
                }
                else
                {
                    Log.Warning("Brush at {Position}: Infinite light radius.", light.Position);
                }

                infinite++;
            }

            // TODO: Extract magic number
            if (light.InnerRadius > 0 && light.Radius - light.InnerRadius > 4)
            {
                if (light.ObjId != -1)
                {
                    Log.Warning(
                        "Object {Id}: High radius to inner-radius differential ({D}). Lightmap may not accurately represent lightgem.",
                        light.ObjId, light.Radius - light.InnerRadius);
                }
                else
                {
                    Log.Warning(
                        "Brush at {Position}: High radius to inner-radius differential ({D}). Lightmap may not accurately represent lightgem.",
                        light.Position, light.Radius - light.InnerRadius);
                }
            }
        }

        if (infinite > 0)
        {
            Log.Warning("Mission contains {Count} infinite lights", infinite);
        }
    }

    private void PostLightValidation()
    {
        if (!_mission.TryGetChunk<WorldRep>("WREXT", out var worldRep))
        {
            return;
        }

        var overLit = 0;
        var overAnimLit = 0;
        var maxLights = 0;
        var maxAnimLights = 0;
        for (var i = 0; i < worldRep.Cells.Length; i++)
        {
            var cell = worldRep.Cells[i];
            if (cell.LightIndexCount > 97)
            {
                Log.Warning("Cell {Id} sees too many lights ({Count}/96)", i, cell.LightIndices[0]);
                overLit++;
            }

            if (cell.AnimLightCount > 32)
            {
                Log.Warning("Cell {Id} sees too many animated lights ({Count}/32)", i, cell.AnimLightCount);
                overAnimLit++;
            }

            if (cell.LightIndexCount > maxLights)
            {
                maxLights = cell.LightIndexCount - 1;
            }

            if (cell.AnimLightCount > maxAnimLights)
            {
                maxAnimLights = cell.AnimLightCount;
            }
        }

        if (overLit > 0)
        {
            Log.Warning(
                "{Count}/{CellCount} cells are overlit. Overlit cells can cause Object/Light Gem lighting issues.",
                overLit, worldRep.Cells.Length);
        }

        if (overAnimLit > 0)
        {
            Log.Warning(
                "{Count}/{CellCount} cells see too many animated lights. When a cell sees too many animated lights some of them will be missing lightmaps.",
                overAnimLit, worldRep.Cells.Length);
        }

        Log.Information("Max cell lights found ({Count}/96)", maxLights);
        Log.Information("Max cell anim lights found ({Count}/32)", maxAnimLights);
    }

    private void ProcessBrushLight(BrList.Brush brush, Settings settings)
    {
        var sz = brush.Size;

        var brightness = Math.Min(sz.X, 255.0f);
        var saturation = sz.Z * settings.Saturation;
        var light = new Light
        {
            Position = brush.Position,
            Color = Utils.HsbToRgb(sz.Y, saturation, brightness),
            Brightness = brightness,
            Hue = sz.Y,
            Saturation = saturation,
            Radius = float.MaxValue,
            R2 = float.MaxValue,
            SpotlightInnerAngle = -1f,
            ObjId = -1
        };

        _lights.Add(light);
    }

    private void ProcessObjectLight(BrList.Brush brush, Settings settings)
    {
        // TODO: Handle PropSpotlightAndAmbient
        var id = (int)brush.BrushInfo;
        var propScale = _hierarchy.GetProperty<PropVector>(id, "P$Scale");
        var propAnimLight = _hierarchy.GetProperty<PropAnimLight>(id, "P$AnimLight", false);
        var propLight = _hierarchy.GetProperty<PropLight>(id, "P$Light", false);
        var propLightColor = _hierarchy.GetProperty<PropLightColor>(id, "P$LightColo");
        var propSpotlight = _hierarchy.GetProperty<PropSpotlight>(id, "P$Spotlight");
        var propSpotAmb = _hierarchy.GetProperty<PropSpotlightAndAmbient>(id, "P$SpotAmb");
        var propModelName = _hierarchy.GetProperty<PropLabel>(id, "P$ModelName");
        var propJointPos = _hierarchy.GetProperty<PropJointPos>(id, "P$JointPos");

        // We don't want to modify the base property info!
        propLightColor ??= new PropLightColor { Hue = 0, Saturation = 0 };
        var lightHue = propLightColor.Hue;
        var lightSaturation = propLightColor.Saturation * settings.Saturation;

        var joints = propJointPos?.Positions ?? [0, 0, 0, 0, 0, 0];

        // Transform data
        var translate = Matrix4x4.CreateTranslation(brush.Position);
        var rotate = Matrix4x4.Identity;
        rotate *= Matrix4x4.CreateRotationX(float.DegreesToRadians(brush.Angle.X));
        rotate *= Matrix4x4.CreateRotationY(float.DegreesToRadians(brush.Angle.Y));
        rotate *= Matrix4x4.CreateRotationZ(float.DegreesToRadians(brush.Angle.Z));
        var scale = Matrix4x4.CreateScale(propScale?.Value ?? Vector3.One);

        var vhotLightPos = Vector3.Zero;
        var vhotLightDir = -Vector3.UnitZ;
        if (propModelName != null && _resources.TryGetModel(propModelName.Value, out var model))
        {
            // We don't need to apply a base transform here because we're applying it later
            var transforms = model.GetObjectTransforms(Matrix4x4.Identity, [..joints]);

            if (model.TryGetVhot(ModelVHotType.LightPosition, out var vhot))
            {
                var objId = model.GetVhotObjectMapping(ModelVHotType.LightPosition);
                var transform = objId != -1 ? transforms[objId] : Matrix4x4.Identity;
                vhotLightPos = Vector3.Transform(vhot.Position, transform) - model.Center;
            }

            if (model.TryGetVhot(ModelVHotType.LightDirection, out vhot))
            {
                var objId = model.GetVhotObjectMapping(ModelVHotType.LightDirection);
                var transform = objId != -1 ? transforms[objId] : Matrix4x4.Identity;
                vhotLightDir = Vector3.Transform(vhot.Position, transform) - model.Center - vhotLightPos;
            }
        }

        if (propAnimLight != null)
        {
            var light = new Light
            {
                Position = propAnimLight.Offset,
                Color = Utils.HsbToRgb(lightHue, lightSaturation, propAnimLight.MaxBrightness),
                Brightness = propAnimLight.MaxBrightness,
                Hue = lightHue,
                Saturation = lightSaturation,
                InnerRadius = propAnimLight.InnerRadius,
                Radius = propAnimLight.Radius,
                R2 = propAnimLight.Radius * propAnimLight.Radius,
                QuadLit = propAnimLight.QuadLit,
                ObjId = id,
                Anim = true,
                Dynamic = propAnimLight.Dynamic,
                SpotlightInnerAngle = -1f
            };

            if (propSpotlight != null)
            {
                light.Spotlight = true;
                light.SpotlightInnerAngle = (float)Math.Cos(float.DegreesToRadians(propSpotlight.InnerAngle));
                light.SpotlightOuterAngle = (float)Math.Cos(float.DegreesToRadians(propSpotlight.OuterAngle));
            }

            light.FixRadius();
            light.ApplyTransforms(vhotLightPos, vhotLightDir, translate, rotate, scale);

            _lights.Add(light);
        }

        if (propLight != null)
        {
            var light = new Light
            {
                Position = propLight.Offset,
                Color = Utils.HsbToRgb(lightHue, lightSaturation, propLight.Brightness),
                Brightness = propLight.Brightness,
                Hue = lightHue,
                Saturation = lightSaturation,
                InnerRadius = propLight.InnerRadius,
                Radius = propLight.Radius,
                R2 = propLight.Radius * propLight.Radius,
                QuadLit = propLight.QuadLit,
                ObjId = id,
                SpotlightInnerAngle = -1f
            };

            if (propSpotAmb != null)
            {
                var spot = new Light
                {
                    Position = light.Position,
                    Color = Utils.HsbToRgb(lightHue, lightSaturation, propSpotAmb.SpotBrightness),
                    Brightness = propSpotAmb.SpotBrightness,
                    Hue = lightHue,
                    Saturation = lightSaturation,
                    InnerRadius = light.InnerRadius,
                    Radius = light.Radius,
                    R2 = light.R2,
                    QuadLit = light.QuadLit,
                    Spotlight = true,
                    SpotlightInnerAngle = (float)Math.Cos(float.DegreesToRadians(propSpotAmb.InnerAngle)),
                    SpotlightOuterAngle = (float)Math.Cos(float.DegreesToRadians(propSpotAmb.OuterAngle)),
                    ObjId = light.ObjId
                };

                spot.FixRadius();
                spot.ApplyTransforms(vhotLightPos, vhotLightDir, translate, rotate, scale);

                _lights.Add(spot);
            }
            else if (propSpotlight != null)
            {
                light.Spotlight = true;
                light.SpotlightInnerAngle = (float)Math.Cos(float.DegreesToRadians(propSpotlight.InnerAngle));
                light.SpotlightOuterAngle = (float)Math.Cos(float.DegreesToRadians(propSpotlight.OuterAngle));
            }

            light.FixRadius();
            light.ApplyTransforms(vhotLightPos, vhotLightDir, translate, rotate, scale);

            _lights.Add(light);
        }
    }

    private void SetCellLightIndices(Settings settings)
    {
        if (!_mission.TryGetChunk<WorldRep>("WREXT", out var worldRep))
        {
            return;
        }

        var cellCount = worldRep.Cells.Length;
        var aabbs = new MathUtils.Aabb[worldRep.Cells.Length];
        Parallel.For(0, cellCount, i => aabbs[i] = new MathUtils.Aabb(worldRep.Cells[i].Vertices));

        var lightCellMap = new int[_lights.Count];
        Parallel.For(0, _lights.Count, i =>
        {
            lightCellMap[i] = -1;
            var light = _lights[i];
            for (var j = 0; j < cellCount; j++)
            {
                if (!MathUtils.Intersects(aabbs[j], light.Position))
                {
                    continue;
                }

                // Half-space contained
                var cell = worldRep.Cells[j];
                var contained = true;
                for (var k = 0; k < cell.PlaneCount; k++)
                {
                    var plane = cell.Planes[k];
                    if (MathUtils.DistanceFromPlane(plane, light.Position) < -MathUtils.Epsilon)
                    {
                        contained = false;
                        break;
                    }
                }

                if (contained)
                {
                    lightCellMap[i] = j;
                    break;
                }
            }

            if (lightCellMap[i] == -1)
            {
                if (light.ObjId != -1)
                {
                    Log.Warning("Object {Id}: Light is inside solid terrain.", light.ObjId);
                }
                else
                {
                    Log.Warning("Brush at {Position}: Light is inside solid terrain.", light.Position);
                }
            }
        });
        Log.Information("Mission has {c} lights", _lights.Count);

        var visibleCellMap = new HashSet<int>[_lights.Count];
        var visGraph = VisGraphBuilder.FromCells(worldRep.Cells);

        Parallel.For(0, _lights.Count, i =>
        {
            var cellIdx = lightCellMap[i];
            if (cellIdx == -1)
            {
                visibleCellMap[i] = [];
                return;
            }

            var visibleSet = visGraph.ComputeVisibleNodes(cellIdx, _lights[i].Position, _lights[i].Radius);
            // Log.Information("Light {i} sees {c} cells", i, visibleSet.Count);
            visibleCellMap[i] = visibleSet;
        });

        // TODO: Move this functionality to the LGS library
        // We set up light indices in separately from lighting because the actual
        // lighting phase takes a lot of shortcuts that we don't want
        // Parallel.ForEach(worldRep.Cells, cell =>
        Parallel.For(0, worldRep.Cells.Length, i =>
        {
            var cell = worldRep.Cells[i];
            cell.LightIndexCount = 0;
            cell.LightIndices.Clear();

            // The first element of the light indices array is used to store how many
            // actual lights are in the list. Which is just LightIndexCount - 1...
            // Odd choice I know
            cell.LightIndexCount++;
            cell.LightIndices.Add(0);

            // If we have sunlight, then we just assume the sun has the potential to reach everything (ew)
            // The sun enabled option doesn't actually seem to do anything at runtime, it's purely about if
            // the cell has the sunlight idx on it.
            if (settings.Sunlight.Enabled)
            {
                cell.LightIndexCount++;
                cell.LightIndices.Add(0);
                cell.LightIndices[0]++;
            }

            // The OG lightmapper uses the cell traversal to work out all the cells that
            // are actually visited. We're a lot more coarse and just say if a cell is
            // in range then we potentially affect the lighting in the cell and add it 
            // to the list.
            // There's a soft length limit here of 96 due to the runtime object shadow
            // cache, so we want this to be as minimal as possible. Additionally large
            // lists actually cause performance issues!
            var cellAabb = new MathUtils.Aabb(cell.Vertices);
            for (var j = 0; j < _lights.Count; j++)
            {
                var light = _lights[j];
                if (light.Dynamic ||
                    !MathUtils.Intersects(new MathUtils.Sphere(light.Position, light.Radius), cellAabb))
                {
                    continue;
                }

                if (!visibleCellMap[j].Contains(i))
                {
                    continue;
                }

                cell.LightIndexCount++;
                cell.LightIndices.Add((ushort)light.LightTableIndex);
                cell.LightIndices[0]++;
            }
        });
    }

    private void TraceScene(Settings settings)
    {
        if (!_mission.TryGetChunk<WorldRep>("WREXT", out var worldRep))
        {
            return;
        }

        Parallel.ForEach(worldRep.Cells, cell =>
        {
            // Reset cell AnimLight palette
            cell.AnimLightCount = 0;
            cell.AnimLights.Clear();

            var numPolys = cell.PolyCount;
            var numRenderPolys = cell.RenderPolyCount;
            var numPortalPolys = cell.PortalPolyCount;
            var solidPolys = numPolys - numPortalPolys;

            var cellIdxOffset = 0;
            for (var polyIdx = 0; polyIdx < numRenderPolys; polyIdx++)
            {
                var poly = cell.Polys[polyIdx];
                var plane = cell.Planes[poly.PlaneId];
                var renderPoly = cell.RenderPolys[polyIdx];
                var info = cell.LightList[polyIdx];
                var lightmap = cell.Lightmaps[polyIdx];

                info.AnimLightBitmask = 0;

                // We have to reset the lightmaps for water, but we don't want to do anything else
                var waterPoly = polyIdx >= solidPolys;
                if (!settings.LightmappedWater && waterPoly)
                {
                    lightmap.Reset(Vector3.One * 255f, settings.Hdr);
                    continue;
                }

                var ambientLight = settings.AmbientLight[cell.ZoneInfo.GetAmbientLightZoneIndex()];
                lightmap.Reset(ambientLight, settings.Hdr);

                // Get world position of lightmap (0, 0) (+0.5 so we cast from the center of a pixel)
                var topLeft = cell.Vertices[cell.Indices[cellIdxOffset]];
                topLeft -= renderPoly.TextureVectors.Item1 * (renderPoly.TextureBases.Item1 - info.Bases.Item1 * 0.25f);
                topLeft -= renderPoly.TextureVectors.Item2 * (renderPoly.TextureBases.Item2 - info.Bases.Item2 * 0.25f);

                var xDir = 0.25f * lightmap.Width * renderPoly.TextureVectors.Item1;
                var yDir = 0.25f * lightmap.Height * renderPoly.TextureVectors.Item2;
                var aabb = new MathUtils.Aabb([
                    topLeft,
                    topLeft + xDir,
                    topLeft + yDir,
                    topLeft + xDir + yDir
                ]);

                // Used for clipping points to poly
                var vs = new Vector3[poly.VertexCount];
                for (var i = 0; i < poly.VertexCount; i++)
                {
                    vs[i] = cell.Vertices[cell.Indices[cellIdxOffset + i]];
                }

                var planeMapper = new MathUtils.PlanePointMapper(plane.Normal, vs[0], vs[1]);
                var v2ds = planeMapper.MapTo2d(vs);

                // TODO: Only need to generate quadweights if there's any quadlights in the mission
                var (texU, texV) = renderPoly.TextureVectors;
                var (offsets, weights) =
                    GetTraceOffsetsAndWeights(settings.MultiSampling, texU, texV, settings.MultiSamplingCenterWeight);
                var (quadOffsets, quadWeights) = settings.MultiSampling != SoftnessMode.Standard
                    ? (offsets, weights)
                    : GetTraceOffsetsAndWeights(SoftnessMode.HighFourPoint, texU, texV,
                        settings.MultiSamplingCenterWeight);

                for (var y = 0; y < lightmap.Height; y++)
                {
                    for (var x = 0; x < lightmap.Width; x++)
                    {
                        var pos = topLeft;
                        pos += x * 0.25f * renderPoly.TextureVectors.Item1;
                        pos += y * 0.25f * renderPoly.TextureVectors.Item2;

                        // TODO: Handle quad lit lights better. Right now we're computing two sets of points for every
                        // luxel. Maybe it's better to only compute if we encounter a quadlit light?
                        var tracePoints = GetTracePoints(pos, offsets, renderPoly.Center, planeMapper, v2ds);
                        var quadTracePoints = settings.MultiSampling != SoftnessMode.Standard
                            ? tracePoints
                            : GetTracePoints(pos, quadOffsets, renderPoly.Center, planeMapper, v2ds);

                        // This is almost perfect now. Any issues seem to be related to Dark not carrying HSB strength correctly
                        if (settings.Sunlight.Enabled)
                        {
                            // Check if plane normal is facing towards the light
                            // If it's not then we're never going to be (directly) lit by this
                            // light.
                            var sunAngle = Vector3.Dot(-settings.Sunlight.Direction, plane.Normal);
                            if (sunAngle > 0)
                            {
                                var strength = 0f;
                                var targetPoints = settings.Sunlight.QuadLit ? quadTracePoints : tracePoints;
                                var targetWeights = settings.Sunlight.QuadLit ? quadWeights : weights;
                                for (var idx = 0; idx < targetPoints.Length; idx++)
                                {
                                    var point = targetPoints[idx];
                                    if (_scene.TraceSun(point, -settings.Sunlight.Direction))
                                    {
                                        // Sunlight is a simpler lighting algorithm than normal lights so we can just
                                        // do it here
                                        strength += targetWeights[idx] * sunAngle;
                                    }
                                }

                                if (strength != 0f)
                                {
                                    lightmap.AddLight(0, x, y, settings.Sunlight.Color, strength, settings.Hdr);
                                }
                            }
                        }

                        // foreach (var lightIdx in cell.LightIndices)
                        for (var i = 0; i < cell.LightIndexCount; i++)
                        {
                            var lightIdx = cell.LightIndices[i];
                            if (i == 0 || lightIdx == 0)
                            {
                                continue;
                            }

                            var light = _lights[lightIdx - 1];

                            // If the light is behind the plane we'll never be directly lit by this light.
                            // Additionally, if the distance from the plane is more than the light's radius
                            // we know no points on the plane will be lit.
                            var planeDist = MathUtils.DistanceFromPlane(plane, light.Position);
                            if (planeDist <= MathUtils.Epsilon || planeDist > light.Radius)
                            {
                                continue;
                            }

                            // If the poly of the lightmap doesn't intersect the light radius then
                            // none of the lightmap points will so we can discard.
                            if (!MathUtils.Intersects(new MathUtils.Sphere(light.Position, light.Radius), aabb))
                            {
                                continue;
                            }

                            var strength = 0f;
                            var targetPoints = light.QuadLit ? quadTracePoints : tracePoints;
                            var targetWeights = light.QuadLit ? quadWeights : weights;
                            for (var idx = 0; idx < targetPoints.Length; idx++)
                            {
                                var point = targetPoints[idx];

                                // If we're out of range there's no point casting a ray
                                // There's probably a better way to discard the entire lightmap
                                // if we're massively out of range
                                if ((point - light.Position).LengthSquared() > light.R2)
                                {
                                    continue;
                                }

                                if (!_scene.TraceOcclusion(SceneTracerType.TerrainAndObjects, light.Position, point))
                                {
                                    strength += targetWeights[idx] * light.StrengthAtPoint(point, plane,
                                        settings.AnimLightCutoff, settings.Attenuation);
                                }
                            }

                            if (strength != 0f)
                            {
                                var layer = 0;

                                // If we're an anim light there's a lot of stuff we need to update
                                // Firstly we need to add the light to the cells anim light palette
                                // Secondly we need to set the appropriate bit of the lightmap's
                                // bitmask. Finally we need to check if the lightmap needs another layer
                                // TODO: Handle too many lights for a layer
                                if (light.Anim)
                                {
                                    // TODO: Don't recalculate this for every point lol
                                    var paletteIdx = cell.AnimLights.IndexOf((ushort)light.LightTableIndex);
                                    if (paletteIdx == -1)
                                    {
                                        paletteIdx = cell.AnimLightCount;
                                        cell.AnimLightCount++;
                                        cell.AnimLights.Add((ushort)light.LightTableIndex);
                                    }

                                    info.AnimLightBitmask |= 1u << paletteIdx;
                                    layer = paletteIdx + 1;
                                }

                                lightmap.AddLight(layer, x, y, light.Color, strength, settings.Hdr);
                            }
                        }
                    }
                }

                cellIdxOffset += poly.VertexCount;
            }
        });
    }

    private static (Vector3[], float[]) GetTraceOffsetsAndWeights(
        SoftnessMode mode,
        Vector3 texU,
        Vector3 texV,
        float centerWeight)
    {
        var offsetScale = mode switch
        {
            SoftnessMode.HighFourPoint or SoftnessMode.HighFivePoint or SoftnessMode.HighNinePoint => 4f,
            SoftnessMode.MediumFourPoint or SoftnessMode.MediumFivePoint or SoftnessMode.MediumNinePoint => 8f,
            SoftnessMode.LowFourPoint => 16f,
            _ => 1f
        };

        var cw = centerWeight;
        var w = 1f - cw;
        texU /= offsetScale;
        texV /= offsetScale;

        return mode switch
        {
            SoftnessMode.LowFourPoint or SoftnessMode.MediumFourPoint or SoftnessMode.HighFourPoint => (
                [-texU - texV, texU - texV, -texU + texV, texU + texV],
                [0.25f, 0.25f, 0.25f, 0.25f]),
            SoftnessMode.MediumFivePoint or SoftnessMode.HighFivePoint => (
                [Vector3.Zero, -texU - texV, texU - texV, -texU + texV, texU + texV],
                [cw, w * 0.25f, w * 0.25f, w * 0.25f, w * 0.25f]),
            SoftnessMode.MediumNinePoint or SoftnessMode.HighNinePoint => (
                [Vector3.Zero, -texU - texV, texU - texV, -texU + texV, texU + texV, -texU, texU, -texV, texV],
                [cw, w * 0.125f, w * 0.125f, w * 0.125f, w * 0.125f, w * 0.125f, w * 0.125f, w * 0.125f, w * 0.125f]),
            _ => (
                [Vector3.Zero],
                [1f])
        };
    }

    private Vector3[] GetTracePoints(
        Vector3 basePosition,
        Vector3[] offsets,
        Vector3 polyCenter,
        MathUtils.PlanePointMapper planeMapper,
        Vector2[] v2ds)
    {
        polyCenter += planeMapper.Normal * 0.25f;

        // All of the traces here are done using the no object scene. We just want to find a point in-world, we don't
        // care about if an object is in the way
        var tracePoints = new Vector3[offsets.Length];
        for (var i = 0; i < offsets.Length; i++)
        {
            var offset = offsets[i];
            var pos = basePosition + offset + planeMapper.Normal * MathUtils.Epsilon;

            // If the target lightmap point is in view of the center
            // then we can use it as-is. Using it straight fixes seams and such.
            if (!_scene.TraceOcclusion(SceneTracerType.Terrain, polyCenter, pos))
            {
                tracePoints[i] = pos;
                continue;
            }

            // If we can't see our target point from the center of the poly
            // then we need to clip the point to slightly inside the poly
            // and retrace to avoid two problems:
            // 1. Darkened spots from lightmap pixels whose center is outside
            //    the polygon but is partially contained in the polygon
            // 2. Darkened spots from linear filtering of points outside the
            //    polygon which have missed
            var p2d = planeMapper.MapTo2d(pos);
            p2d = MathUtils.ClipPointToPoly2d(p2d, v2ds);
            pos = planeMapper.MapTo3d(p2d);
            pos += planeMapper.Normal * MathUtils.Epsilon;

            // If the clipping fails, just say screw it and cast :(
            if (_scene.TraceOcclusion(SceneTracerType.Terrain, polyCenter, pos))
            {
                var hit = _scene.Trace(SceneTracerType.Terrain, polyCenter, pos - polyCenter);
                if (hit)
                {
                    pos = hit.Position;
                }
            }

            tracePoints[i] = pos;
        }

        return tracePoints;
    }

    private void SetAnimLightCellMaps()
    {
        if (!_mission.TryGetChunk<PropertyChunk<PropAnimLight>>("P$AnimLight", out var animLightChunk) ||
            !_mission.TryGetChunk<WorldRep>("WREXT", out var worldRep))
        {
            return;
        }

        // Now that we've set all the per-cell stuff we need to aggregate the cell mappings
        // We can't do this in parallel which is why it's being done afterwards rather than
        // as we go
        var map = new Dictionary<ushort, List<WorldRep.LightTable.AnimCellMap>>();
        for (ushort i = 0; i < worldRep.Cells.Length; i++)
        {
            var cell = worldRep.Cells[i];
            for (ushort j = 0; j < cell.AnimLightCount; j++)
            {
                var animLightIdx = cell.AnimLights[j];
                if (!map.TryGetValue(animLightIdx, out var value))
                {
                    value = [];
                    map[animLightIdx] = value;
                }

                value.Add(new WorldRep.LightTable.AnimCellMap
                {
                    CellIndex = i,
                    LightIndex = j
                });
            }
        }

        foreach (var (lightIdx, animCellMaps) in map)
        {
            // We need to update the object property so it knows its mapping range
            // TODO: Handle nulls
            var light = _lights.Find(l => l.Anim && l.LightTableIndex == lightIdx);
            var prop = animLightChunk.Properties.Find(p => p.ObjectId == light.ObjId);
            prop.LightTableLightIndex = lightIdx;
            prop.LightTableMapIndex = (ushort)worldRep.LightingTable.AnimMapCount;
            prop.CellsReached = (ushort)animCellMaps.Count;

            if (worldRep.LightingTable.AnimMapCount + animCellMaps.Count >= WorldRep.LightTable.MaxAnimMapCount)
            {
                prop.CellsReached = 0;
                Log.Warning("AnimCellMapping reached. Light {Id} cannot update runtime lightmap levels.", light.ObjId);
                continue;
            }

            worldRep.LightingTable.AnimCellMaps.AddRange(animCellMaps);
            worldRep.LightingTable.AnimMapCount += animCellMaps.Count;
        }
    }
}