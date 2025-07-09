namespace KeepersCompound.Formats;

public interface IBinaryParser<out T>
{
    public T? Read(BinaryReader reader);
    public void Write(BinaryWriter writer);
}