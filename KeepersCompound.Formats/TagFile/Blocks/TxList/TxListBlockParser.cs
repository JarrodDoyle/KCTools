namespace KeepersCompound.Formats.TagFile.Blocks.TxList;

public class TxListBlockParser : IBinaryParser<AbstractBlock>
{
    public AbstractBlock Read(BinaryReader reader)
    {
        var header = new BlockHeaderParser().Read(reader);
        var blockSize = reader.ReadInt32();
        var itemCount = reader.ReadInt32();
        var tokenCount = reader.ReadInt32();
        var tokens = new string[tokenCount];
        for (var i = 0; i < tokenCount; i++)
        {
            tokens[i] = reader.ReadNullString(16);
        }

        var items = new TxListItem[itemCount];
        for (var i = 0; i < itemCount; i++)
        {
            items[i] = new TxListItem
            {
                Tokens = reader.ReadBytes(4),
                Name = reader.ReadNullString(16),
            };
        }

        return new TxListBlock
        {
            Header = header,
            BlockSize = blockSize,
            ItemCount = itemCount,
            TokenCount = tokenCount,
            Tokens = tokens,
            Items = items
        };
    }

    public void Write(BinaryWriter writer, AbstractBlock item)
    {
        if (item is not TxListBlock block)
        {
            return;
        }

        writer.Write(block.BlockSize);
        writer.Write(block.ItemCount);
        writer.Write(block.TokenCount);
        foreach (var token in block.Tokens)
        {
            writer.WriteNullString(token, 16);
        }

        foreach (var txItem in block.Items)
        {
            writer.Write(txItem.Tokens);
            writer.WriteNullString(txItem.Name, 16);
        }
    }
}