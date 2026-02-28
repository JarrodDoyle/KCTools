using KeepersCompound.Dark.Database.Chunks;

namespace KeepersCompound.Dark.Portalisation;

public static class CsgMediaTable
{
    private static readonly CsgMedia[][] Table =
    [
        [
            CsgMedia.Solid, CsgMedia.Solid, CsgMedia.Solid,
            CsgMedia.Solid, CsgMedia.Solid, CsgMedia.Solid,
        ],
        [
            CsgMedia.Air, CsgMedia.Air, CsgMedia.Air,
            CsgMedia.AirPersist, CsgMedia.AirPersist, CsgMedia.AirPersist,
        ],
        [
            CsgMedia.Water, CsgMedia.Water, CsgMedia.Water,
            CsgMedia.WaterPersist, CsgMedia.WaterPersist, CsgMedia.WaterPersist,
        ],
        [
            CsgMedia.Solid, CsgMedia.Water, CsgMedia.Water,
            CsgMedia.SolidPersist, CsgMedia.WaterPersist, CsgMedia.WaterPersist,
        ],
        [
            CsgMedia.Solid, CsgMedia.Air, CsgMedia.Air,
            CsgMedia.SolidPersist, CsgMedia.AirPersist, CsgMedia.AirPersist,
        ],
        [
            CsgMedia.Water, CsgMedia.Air, CsgMedia.Water,
            CsgMedia.WaterPersist, CsgMedia.AirPersist, CsgMedia.WaterPersist,
        ],
        [
            CsgMedia.Air, CsgMedia.Air, CsgMedia.Water,
            CsgMedia.AirPersist, CsgMedia.AirPersist, CsgMedia.WaterPersist,
        ],
        [
            CsgMedia.Solid, CsgMedia.Solid, CsgMedia.Water,
            CsgMedia.SolidPersist, CsgMedia.SolidPersist, CsgMedia.WaterPersist,
        ],
        [
            CsgMedia.Solid, CsgMedia.Air, CsgMedia.Solid,
            CsgMedia.SolidPersist, CsgMedia.AirPersist, CsgMedia.SolidPersist,
        ],
        [
            CsgMedia.Solid, CsgMedia.AirPersist, CsgMedia.WaterPersist,
            CsgMedia.Solid, CsgMedia.AirPersist, CsgMedia.WaterPersist,
        ]
    ];

    public static CsgMedia GetMedium(Media operation, CsgMedia currentMedium)
    {
        return Table[(int)operation][(int)currentMedium];
    }
}