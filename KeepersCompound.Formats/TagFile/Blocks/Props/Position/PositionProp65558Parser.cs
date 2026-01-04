namespace KeepersCompound.Formats.TagFile.Blocks.Props.Position;

public class PositionProp65558Parser : IBinaryParser<PositionProp>
{
    public PositionProp Read(BinaryReader reader)
    {
        var objectId = reader.ReadInt32();
        var length = (int)reader.ReadUInt32();
        var location = reader.ReadVec3();
        var cellHint = reader.ReadInt32();
        var rotation = reader.ReadRotation();

        return new PositionProp
        {
            ObjectId = objectId,
            Length = length,
            Location = location,
            CellHint = cellHint,
            Rotation = rotation
        };
    }

    public void Write(BinaryWriter writer, PositionProp item)
    {
        writer.Write(item.ObjectId);
        writer.Write((uint)item.Length);
        writer.WriteVec3(item.Location);
        writer.Write(item.CellHint);
        writer.WriteRotation(item.Rotation);
    }
}