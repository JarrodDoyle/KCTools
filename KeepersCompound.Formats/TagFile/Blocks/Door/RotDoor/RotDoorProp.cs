using System.Numerics;

namespace KeepersCompound.Formats.TagFile.Blocks.Door.RotDoor;

public class RotDoorProp
{
    public int ObjectId;
    public int Length;
    public required int Type { get; set; }
    public required float ClosedAngle { get; set; }
    public required float OpenAngle { get; set; }
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
    public required bool Clockwise { get; set; }
    public required Vector3 ClosedRotation { get; set; }
    public required Vector3 OpenRotation { get; set; }
    public required float LeanBlocksSoundPct { get; set; }
}
