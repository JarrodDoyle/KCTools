using System.Numerics;
using KeepersCompound.Dark.Database.Chunks;

namespace KeepersCompound.Lighting;

public static class LightMapperSettingsBuilder
{
    public static LightMapperSettings FromChunks(WorldRep worldRep, RendParams rendParams, LmParams lmParams)
    {
        var sunlightSettings = new LightMapperSunSettings
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
        var settings = new LightMapperSettings
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

        return settings;
    }
}