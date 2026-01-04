using KeepersCompound.Formats.TagFile.Blocks.GamFile;
using KeepersCompound.Formats.TagFile.Blocks.LmParams;
using KeepersCompound.Formats.TagFile.Blocks.Props.Door.RenderType;
using KeepersCompound.Formats.TagFile.Blocks.Props.Door.RotDoor;
using KeepersCompound.Formats.TagFile.Blocks.Props.Door.TransDoor;
using KeepersCompound.Formats.TagFile.Blocks.Unknown;

namespace KeepersCompound.Formats.TagFile.Blocks;

public static class BlockParserMapper
{
    public static IBinaryParser<AbstractBlock> GetBlockParser(TocEntry entry)
    {
        return entry.Tag switch
        {
            "GAM_FILE" => new GamFileBlockParser(),
            "LM_PARAM" => new LmParamsBlockParser(),
            "P$TransDoor" => new TransDoorBlockParser(entry),
            "P$RenderTyp" => new RenderTypeBlockParser(entry),
            "P$RotDoor" => new RotDoorBlockParser(entry),
            _ => new UnknownBlockParser(entry),
        };
    }
}