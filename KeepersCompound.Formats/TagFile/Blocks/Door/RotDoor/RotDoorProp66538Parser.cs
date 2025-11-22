namespace KeepersCompound.Formats.TagFile.Blocks.Door.RotDoor;

public class RotDoorProp66538Parser : IBinaryParser<RotDoorProp>
{
    public RotDoorProp Read(BinaryReader reader)
    {
        var objectId = reader.ReadInt32();
        var length = (int)reader.ReadUInt32();
        var type = reader.ReadInt32();
        var closedAngle = reader.ReadSingle();
        var openAngle = reader.ReadSingle();
        var baseSpeed = reader.ReadSingle();
        var axis = (DoorAxis)reader.ReadInt32();
        var status = (DoorStatus)reader.ReadInt32();
        var hardLimits = reader.ReadBoolean();
        reader.ReadBytes(3);
        var blocksSoundPct = reader.ReadSingle();
        var blocksVision = reader.ReadBoolean();
        reader.ReadBytes(3);
        var pushMass = reader.ReadSingle();
        var closedPosition = reader.ReadVec3();
        var openPosition = reader.ReadVec3();
        var position = reader.ReadVec3();
        var rotation = reader.ReadRotation();
        var baseRotation = reader.ReadSingle();
        var roomHint1 = reader.ReadInt32();
        var roomHint2 = reader.ReadInt32();
        var clockwise = reader.ReadBoolean();
        reader.ReadBytes(3);
        var closedRotation = reader.ReadRotation();
        var openRotation = reader.ReadRotation();
        var leanBlocksSoundPct = reader.ReadSingle();

        return new RotDoorProp
        {
            ObjectId = objectId,
            Length = length,
            Type = type,
            ClosedAngle = closedAngle,
            OpenAngle = openAngle,
            BaseSpeed = baseSpeed,
            Axis = axis,
            Status = status,
            HardLimits = hardLimits,
            BlocksSoundPct = blocksSoundPct,
            BlocksVision = blocksVision,
            PushMass = pushMass,
            ClosedPosition = closedPosition,
            OpenPosition = openPosition,
            Position = position,
            Position2 = default,
            Rotation = rotation,
            Base = baseRotation,
            RoomHint1 = roomHint1,
            RoomHint2 = roomHint2,
            Clockwise = clockwise,
            ClosedRotation = closedRotation,
            OpenRotation = openRotation,
            LeanBlocksSoundPct = leanBlocksSoundPct,
        };
    }

    public void Write(BinaryWriter writer, RotDoorProp item)
    {
        writer.Write(item.ObjectId);
        writer.Write((uint)item.Length);
        writer.Write(item.Type);
        writer.Write(item.ClosedAngle);
        writer.Write(item.OpenAngle);
        writer.Write(item.BaseSpeed);
        writer.Write((int)item.Axis);
        writer.Write((int)item.Status);
        writer.Write(item.HardLimits);
        writer.Write(new byte[3]);
        writer.Write(item.BlocksSoundPct);
        writer.Write(item.BlocksVision);
        writer.Write(new byte[3]);
        writer.Write(item.PushMass);
        writer.WriteVec3(item.ClosedPosition);
        writer.WriteVec3(item.OpenPosition);
        writer.WriteVec3(item.Position);
        writer.WriteRotation(item.Rotation);
        writer.Write(item.Base);
        writer.Write(item.RoomHint1);
        writer.Write(item.RoomHint2);
        writer.Write(item.Clockwise);
        writer.Write(new byte[3]);
        writer.WriteRotation(item.ClosedRotation);
        writer.WriteRotation(item.OpenRotation);
        writer.Write(item.LeanBlocksSoundPct);
    }
}