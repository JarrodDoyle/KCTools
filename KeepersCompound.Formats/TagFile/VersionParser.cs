using Serilog;

namespace KeepersCompound.Formats.TagFile;

public class VersionParser : IBinaryParser<Version>
{
    public Version Read(BinaryReader reader)
    {
        return new Version(reader.ReadUInt32(), reader.ReadUInt32());
    }

    public void Write(BinaryWriter writer, Version item)
    {
        writer.Write(item.Major);
        writer.Write(item.Minor);
    }
}