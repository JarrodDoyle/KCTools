using System.Diagnostics.CodeAnalysis;
using System.Text;
using KeepersCompound.Formats.Db.Chunks;

namespace KeepersCompound.Formats.Db;

public class DbFile
{
    public Version Version { get; set; }
    public string DeadBeef { get; set; }
    public List<TocEntry> TocEntries { get; set; }
    public List<IChunk> Chunks { get; set; }

    public DbFile(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);

        // Header
        var tocOffset = reader.ReadUInt32();
        Version = new Version(reader);
        reader.ReadBytes(256);
        DeadBeef = BitConverter.ToString(reader.ReadBytes(4));

        // Table of contents
        reader.BaseStream.Seek(tocOffset, SeekOrigin.Begin);
        var entryCount = reader.ReadUInt32();
        TocEntries = new List<TocEntry>((int)entryCount);
        for (var i = 0; i < entryCount; i++)
        {
            TocEntries.Add(new TocEntry(
                Name: reader.ReadNullString(12),
                Offset: reader.ReadUInt32(),
                Size: reader.ReadUInt32()
            ));
        }

        // Chunks
        Chunks = new List<IChunk>((int)entryCount);
        foreach (var entry in TocEntries)
        {
            var chunk = NewChunk(entry.Name);
            chunk.Read(reader, entry);
            Chunks.Add(chunk);
        }
    }

    public void Write(Stream stream)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);

        // Offset is left blank in the header for now, will be filled in later
        writer.Write(new byte[4]);
        Version.Write(writer);
        writer.Write(new byte[256]);
        writer.Write(Array.ConvertAll(DeadBeef.Split('-'),
            s => byte.Parse(s, System.Globalization.NumberStyles.HexNumber)));

        // Chunks
        TocEntries.Clear();
        foreach (var chunk in Chunks)
        {
            var name = chunk.Name;
            var offset = writer.BaseStream.Position;
            chunk.Write(writer);

            // Entry size doesn't include the fixed-length of the chunk header
            var size = writer.BaseStream.Position - offset - 24;

            TocEntries.Add(new TocEntry(name, (uint)offset, (uint)size));
        }

        // Table of contents
        var tocOffset = (uint)writer.BaseStream.Position;
        writer.Write((uint)TocEntries.Count);
        foreach (var entry in TocEntries)
        {
            writer.WriteNullString(entry.Name, 12);
            writer.Write(entry.Offset);
            writer.Write(entry.Size);
        }

        // Backfill the toc offset now that we know it
        stream.Seek(0, SeekOrigin.Begin);
        writer.Write(tocOffset);
    }

    public bool TryGetChunk<T>([MaybeNullWhen(false)] out T chunk) where T : IChunk
    {
        chunk = (T?)Chunks.FirstOrDefault(e => e is T);
        return chunk != null;
    }

    private static IChunk NewChunk(string entryName)
    {
        return entryName switch
        {
            _ => new GenericChunk(),
        };
    }
}