namespace KeepersCompound.Formats.TagFile.Blocks.Props.Bool;

public class BoolProp4Parser<T> : IBinaryParser<T> where T : BoolProp, IFromType<T, BoolProp>
{
    public T Read(BinaryReader reader)
    {
        return T.FromType(new BoolProp
        {
            ObjectId = reader.ReadInt32(),
            Length = (int)reader.ReadUInt32(),
            Value = reader.ReadInt32() != 0
        });
    }

    public void Write(BinaryWriter writer, T item)
    {
        writer.Write(item.ObjectId);
        writer.Write((uint)item.Length);
        writer.Write(item.Value ? 1 : 0);
    }
}