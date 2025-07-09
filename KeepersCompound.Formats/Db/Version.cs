namespace KeepersCompound.Formats.Db;

public class Version
{
    public uint Major { get; set; }
    public uint Minor { get; set; }

    public Version(BinaryReader reader)
    {
        Major = reader.ReadUInt32();
        Minor = reader.ReadUInt32();
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(Major);
        writer.Write(Minor);
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}";
    }
}