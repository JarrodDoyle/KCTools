namespace KeepersCompound.Formats.TagFile.Blocks.Props.Door.TransDoor;

public class TransDoorProp66538Parser : IBinaryParser<TransDoorProp>
{
    public TransDoorProp Read(BinaryReader reader)
    {
        var objectId = reader.ReadInt32();
        var length = (int)reader.ReadUInt32();
        var type = reader.ReadInt32();
        var closedOffset = reader.ReadSingle();
        var openOffset = reader.ReadSingle();
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
        var position2 = reader.ReadVec3();
        var rotation = reader.ReadRotation();
        var baseRotation = reader.ReadSingle();
        var roomHint1 = reader.ReadInt32();
        var roomHint2 = reader.ReadInt32();
        var leanBlocksSoundPct = reader.ReadSingle();

        return new TransDoorProp
        {
            ObjectId = objectId,
            Length = length,
            Type = type,
            ClosedOffset = closedOffset,
            OpenOffset = openOffset,
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
            Position2 = position2,
            Rotation = rotation,
            Base = baseRotation,
            RoomHint1 = roomHint1,
            RoomHint2 = roomHint2,
            LeanBlocksSoundPct = leanBlocksSoundPct
        };
    }

    public void Write(BinaryWriter writer, TransDoorProp item)
    {
        writer.Write(item.ObjectId);
        writer.Write((uint)item.Length);
        writer.Write(item.Type);
        writer.Write(item.ClosedOffset);
        writer.Write(item.OpenOffset);
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
        writer.WriteVec3(item.Position2);
        writer.WriteRotation(item.Rotation);
        writer.Write(item.Base);
        writer.Write(item.RoomHint1);
        writer.Write(item.RoomHint2);
        writer.Write(item.LeanBlocksSoundPct);
    }
}