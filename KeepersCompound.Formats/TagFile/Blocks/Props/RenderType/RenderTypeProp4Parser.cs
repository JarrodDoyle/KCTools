namespace KeepersCompound.Formats.TagFile.Blocks.Props.RenderType;

public class RenderTypeProp4Parser : IBinaryParser<RenderTypeProp>
{
    public RenderTypeProp Read(BinaryReader reader)
    {
        var objectId = reader.ReadInt32();
        var length = (int)reader.ReadUInt32();
        var renderType = (RenderType)reader.ReadInt32();

        return new RenderTypeProp
        {
            ObjectId = objectId,
            Length = length,
            RenderType = renderType,
        };
    }

    public void Write(BinaryWriter writer, RenderTypeProp item)
    {
        writer.Write(item.ObjectId);
        writer.Write((uint)item.Length);
        writer.Write((int)item.RenderType);
    }
}