namespace KeepersCompound.Formats;

public interface IBinaryParser<T>
{
    public T Read(BinaryReader reader);
    public void Write(BinaryWriter writer, T item);
}