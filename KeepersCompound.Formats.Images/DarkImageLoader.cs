using System.Diagnostics.CodeAnalysis;
using DmitryBrant.ImageFormats;
using Pfim;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace KeepersCompound.Formats.Images;

public static class DarkImageLoader
{
    public static bool TryLoadImage(Stream stream, string ext, [MaybeNullWhen(false)] out DarkImage image)
    {
        // TODO: Handle external palettes
        var rawImage = ext switch
        {
            ".dds" => LoadPfim(stream),
            ".png" => Image.Load(stream),
            ".tga" => LoadPfim(stream),
            ".bmp" => Image.Load(stream),
            ".pcx" => PcxReader.Load(stream, false, 0),
            ".gif" => LoadGif(stream),
            _ => null
        };

        if (rawImage == null)
        {
            image = null;
            return false;
        }

        var pngStream = new MemoryStream();
        rawImage.Save(pngStream, PngFormat.Instance);
        image = new DarkImage(rawImage.Width, rawImage.Height, pngStream);
        return true;
    }

    private static Image? LoadPfim(Stream stream)
    {
        using var image = Pfimage.FromStream(stream);

        // Since image sharp can't handle data with line padding in a stride
        // we create a stripped down array if any padding is detected
        byte[] newData;
        var tightStride = image.Width * image.BitsPerPixel / 8;
        if (image.Stride != tightStride)
        {
            newData = new byte[image.Height * tightStride];
            for (var i = 0; i < image.Height; i++)
            {
                Buffer.BlockCopy(image.Data, i * image.Stride, newData, i * tightStride, tightStride);
            }
        }
        else
        {
            newData = image.Data;
        }

        switch (image.Format)
        {
            case ImageFormat.Rgb8:
                return Image.LoadPixelData<L8>(newData, image.Width, image.Height);
            case ImageFormat.R5g5b5:
                for (var i = 1; i < newData.Length; i += 2)
                {
                    newData[i] |= 128;
                }

                return Image.LoadPixelData<Bgra5551>(newData, image.Width, image.Height);
            case ImageFormat.R5g6b5:
                return Image.LoadPixelData<Bgr565>(newData, image.Width, image.Height);
            case ImageFormat.R5g5b5a1:
                return Image.LoadPixelData<Bgra5551>(newData, image.Width, image.Height);
            case ImageFormat.Rgba16:
                return Image.LoadPixelData<Bgra4444>(newData, image.Width, image.Height);
            case ImageFormat.Rgb24:
                return Image.LoadPixelData<Bgr24>(newData, image.Width, image.Height);
            case ImageFormat.Rgba32:
                return Image.LoadPixelData<Bgra32>(newData, image.Width, image.Height);
            case ImageFormat.R16f:
                return Image.LoadPixelData<HalfSingle>(newData, image.Width, image.Height);
            case ImageFormat.R32f:
            default:
                return null;
        }
    }

    private static Image LoadGif(Stream stream)
    {
        var image = Image.Load(stream);
        var meta = image.Metadata.GetGifMetadata();
        meta.BackgroundColorIndex = 0;

        for (var i = image.Frames.Count - 1; i > 0; i--)
        {
            image.Frames.RemoveFrame(i);
        }

        return image;
    }
}