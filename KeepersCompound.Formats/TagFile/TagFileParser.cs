using KeepersCompound.Formats.TagFile.Blocks;
using Serilog;

namespace KeepersCompound.Formats.TagFile;

public class TagFileParser : IBinaryParser<TagFile>
{
    public TagFile? Read(BinaryReader reader)
    {
        try
        {
            var tocOffset = reader.ReadUInt32();
            var version = new VersionParser().Read(reader)!;
            reader.ReadBytes(256);
            var deadBeef = BitConverter.ToString(reader.ReadBytes(4));
            reader.BaseStream.Seek(tocOffset, SeekOrigin.Begin);
            var entryCount = reader.ReadUInt32();
            var tocEntries = new List<TocEntry>((int)entryCount);
            for (var i = 0; i < entryCount; i++)
            {
                tocEntries.Add(new TocEntry(
                    Tag: reader.ReadNullString(12),
                    Offset: reader.ReadUInt32(),
                    Size: reader.ReadUInt32()
                ));
            }

            var blocks = new Dictionary<string, AbstractBlock>((int)entryCount);
            foreach (var entry in tocEntries)
            {
                reader.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
                blocks.Add(entry.Tag, BlockParserMapper.GetBlockParser(entry).Read(reader));
            }

            return new TagFile
            {
                TocOffset = tocOffset,
                Version = version,
                DeadBeef = deadBeef,
                TocEntries = tocEntries,
                Blocks = blocks,
            };
        }
        catch (Exception e)
        {
            Log.Error("Failed to parse TagFile: {e}", e);
            return null;
        }
    }

    public void Write(BinaryWriter writer, TagFile item)
    {
        // Have to write in a different order than reading because Block sizes (and therefore offsets) may have changed
        // Offset is left blank in the header for now, will be filled in later
        writer.Write(new byte[4]);
        new VersionParser().Write(writer, item.Version);
        writer.Write(new byte[256]);
        writer.Write(Array.ConvertAll(item.DeadBeef.Split('-'),
            s => byte.Parse(s, System.Globalization.NumberStyles.HexNumber)));

        item.TocEntries.Clear();
        foreach (var block in item.Blocks.Values)
        {
            var tag = block.Header.Tag;
            var offset = writer.BaseStream.Position;
            var entry = new TocEntry(tag, (uint)offset, 0);
            BlockParserMapper.GetBlockParser(entry).Write(writer, block);

            var size = writer.BaseStream.Position - offset - 24;
            item.TocEntries.Add(new TocEntry(tag, (uint)offset, (uint)size));
        }

        item.TocOffset = (uint)writer.BaseStream.Position;
        writer.Write((uint)item.TocEntries.Count);
        foreach (var entry in item.TocEntries)
        {
            writer.WriteNullString(entry.Tag, 12);
            writer.Write(entry.Offset);
            writer.Write(entry.Size);
        }

        // Backfill the toc offset now that we know it
        writer.BaseStream.Seek(0, SeekOrigin.Begin);
        writer.Write(item.TocOffset);
    }
}