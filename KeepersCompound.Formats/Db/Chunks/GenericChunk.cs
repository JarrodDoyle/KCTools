namespace KeepersCompound.Formats.Db.Chunks;

public class GenericChunk : IChunk
{
    public string Name { get; set; }
    public Version Version { get; set; }
    public byte[] Data { get; set; } = [];

    public void ReadData(BinaryReader reader, TocEntry entry)
    {
        Data = reader.ReadBytes((int)entry.Size);
    }

    public void WriteData(BinaryWriter writer)
    {
        writer.Write(Data);
    }
}