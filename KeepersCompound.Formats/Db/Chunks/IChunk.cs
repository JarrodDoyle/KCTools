using System.Text;

namespace KeepersCompound.Formats.Db.Chunks;

public interface IChunk
{
    public string Name { get; set; }
    public Version Version { get; set; }

    public void Read(BinaryReader reader, TocEntry entry)
    {
        reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);

        Name = reader.ReadNullString(12);
        Version = new Version(reader);
        reader.ReadBytes(4);

        ReadData(reader, entry);
    }

    public void Write(BinaryWriter writer)
    {
        var writeBytes = new byte[12];
        var nameBytes = Encoding.UTF8.GetBytes(Name);
        nameBytes[..Math.Min(12, nameBytes.Length)].CopyTo(writeBytes, 0);
        writer.Write(writeBytes);
        Version.Write(writer);
        writer.Write(new byte[4]);

        WriteData(writer);
    }

    public void ReadData(BinaryReader reader, TocEntry entry);
    public void WriteData(BinaryWriter writer);
}