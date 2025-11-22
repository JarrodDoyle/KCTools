using System.Numerics;

namespace KeepersCompound.Formats.TagFile.Blocks.Door.TransDoor;

public class TransDoorProp
{
    public required int ObjectId { get; set; }
    public required int Length { get; set; }
    public required int Type { get; set; }
    public required float ClosedOffset { get; set; }
    public required float OpenOffset { get; set; }
    public required float BaseSpeed { get; set; }
    public required DoorAxis Axis { get; set; }
    public required DoorStatus Status { get; set; }
    public required bool HardLimits { get; set; }
    public required float BlocksSoundPct { get; set; }
    public required bool BlocksVision { get; set; }
    public required float PushMass { get; set; }
    public required Vector3 ClosedPosition { get; set; }
    public required Vector3 OpenPosition { get; set; }
    public required Vector3 Position { get; set; }
    public required Vector3 Position2 { get; set; }
    public required Vector3 Rotation { get; set; }
    public required float Base { get; set; }
    public required int RoomHint1 { get; set; }
    public required int RoomHint2 { get; set; }
    public required float LeanBlocksSoundPct { get; set; }
}