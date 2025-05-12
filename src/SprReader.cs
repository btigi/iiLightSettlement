using ii.LightSettlement.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class SprProcessor
{
    /// <summary>
    /// Reads .spr, .b00 and .jus files and returns a list of images.
    /// </summary>
    public List<Image<Rgba32>> Read(string fileName)
    {
        // 0x000 Header  
        // 0x008 Palette  
        // 0x308 Frame metadata  
        //       Frame data (if compressed, first int is the length of the data)
        const int FrameMetadataOffset = 776;
        const int FrameMetadataSize = 8;

        var result = new List<Image<Rgba32>>();

        using (var br = new BinaryReader(File.OpenRead(fileName)))
        {
            var compressed = (br.ReadInt16() & (1 << 7)) != 0;
            var frameCount = br.ReadInt16();

            var frameDataOffset = FrameMetadataOffset + (frameCount * FrameMetadataSize);

            _ = br.ReadInt32();

            var palette = new List<RGBA>();
            for (int i = 0; i < 256; i++)
            {
                palette.Add(new RGBA
                {
                    R = (byte)(br.ReadByte() * 4 + 3),
                    G = (byte)(br.ReadByte() * 4 + 3),
                    B = (byte)(br.ReadByte() * 4 + 3),
                    A = 255
                });
            }

            var runningOffset = 0;
            var frames = new List<SprFrame>();
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var frame = new SprFrame();

                var infoOffset = FrameMetadataOffset + (frameIndex * FrameMetadataSize);
                br.BaseStream.Seek(infoOffset, SeekOrigin.Begin);
                frame.Width = br.ReadInt16();
                frame.Height = br.ReadInt16();
                frame.DisX = br.ReadInt16();
                frame.DisY = br.ReadInt16();

                frame.Start = frameDataOffset + runningOffset;

                if (compressed)
                {
                    // Compressed frame use the first 4 bytes of the 'frame data' to indicate the length  
                    // We need to go read that info now in order to maintain runningOffset  
                    br.BaseStream.Seek(frame.Start, SeekOrigin.Begin);
                    frame.BytesInThisFrame = br.ReadInt32();
                    runningOffset += frame.BytesInThisFrame + 4; // '4' to account for the 'data length' int
                }
                else
                {
                    frame.BytesInThisFrame = frame.Width * frame.Height;
                    runningOffset += frame.BytesInThisFrame;
                }

                frames.Add(frame);
            }

            foreach (var frame in frames)
            {
                br.BaseStream.Seek(frame.Start, SeekOrigin.Begin);
                var imageData = br.ReadBytes(frame.BytesInThisFrame);
                var bitmap = ConvertToBitmap(imageData, frame.Width, frame.Height, compressed, palette);
                result.Add(bitmap);
            }
        }

        return result;
    }

    Image<Rgba32> ConvertToBitmap(byte[] imageData, int width, int height, bool compressed, List<RGBA> palette)
    {
        var image = new Image<Rgba32>(width, height);
        if (compressed)
        {
            // Walk through imageData (ignoring the first 4 bytes, which are the length of the data)  
            // If the byte is < 128, it means the next byte is a palette index  
            // If the byte is >= 128, it means the next byte is a count of black pixels  
            var currentByteIndex = 4;
            var pixelIndex = 0;
            while (currentByteIndex < imageData.Length)
            {
                var currentByte = imageData[currentByteIndex];
                if (currentByte < 128)
                {
                    var rawByteCount = currentByte + 1;
                    for (var i = 1; i <= rawByteCount; i++)
                    {
                        if (currentByteIndex + i < imageData.Length)
                        {
                            var paletteIndex = imageData[currentByteIndex + i];
                            var rgba = palette[paletteIndex];
                            image[pixelIndex % width, pixelIndex / width] = new Rgba32(rgba.R, rgba.G, rgba.B, rgba.A);
                        }
                        pixelIndex++;
                    }
                    currentByteIndex += rawByteCount + 1;
                }
                else
                {
                    var BlackByteCount = 256 - currentByte;
                    for (var i = 0; i < BlackByteCount; i++)
                    {
                        image[pixelIndex % width, pixelIndex / width] = new Rgba32(0, 0, 0, 255);
                        pixelIndex++;
                    }
                    currentByteIndex++;
                }
            }
        }
        else
        {
            // Every byte is a palette index  
            var pixelIndex = 0;
            for (int c = 0; c < imageData.Length; c++)
            {
                var paletteIndex = imageData[c];
                var rgba = palette[paletteIndex];
                image[pixelIndex % width, pixelIndex / width] = new Rgba32(rgba.R, rgba.G, rgba.B, rgba.A);
                pixelIndex++;
            }
        }

        return image;
    }

    public void Write(string fileName, List<Image<Rgba32>> images, bool compressed)
    {
        if (compressed)
            throw new NotImplementedException("Saving compressed files is not currently supported");

        const int FrameMetadataOffset = 776;
        const int FrameMetadataSize = 8;

        using var bw = new BinaryWriter(File.Open(fileName, FileMode.Create));

        bw.Write((short)(compressed ? 1 << 7 : 0)); // Compression flag
        bw.Write((short)images.Count); // Frame count
        bw.Write(0); // Unknown

        var palette = GeneratePalette(images);
        foreach (var color in palette)
        {
            bw.Write((byte)((color.R - 3) / 4));
            bw.Write((byte)((color.G - 3) / 4));
            bw.Write((byte)((color.B - 3) / 4));
        }

        var frameDataOffset = FrameMetadataOffset + (images.Count * FrameMetadataSize);
        var runningOffset = 0;

        for (int frameIndex = 0; frameIndex < images.Count; frameIndex++)
        {
            var image = images[frameIndex];

            bw.Seek(FrameMetadataOffset + (frameIndex * FrameMetadataSize), SeekOrigin.Begin);
            bw.Write((short)image.Width);
            bw.Write((short)image.Height);
            bw.Write((short)0); // DisX
            bw.Write((short)0); // DisY

            bw.Seek(frameDataOffset + runningOffset, SeekOrigin.Begin);
            var imageData = ConvertFromBitmap(image, compressed, palette);
            if (compressed)
            {
                bw.Write(imageData.Length);
                bw.Write(imageData);
                runningOffset += imageData.Length + 4; // Include length prefix
            }
            else
            {
                bw.Write(imageData);
                runningOffset += imageData.Length;
            }
        }
    }

    private List<RGBA> GeneratePalette(List<Image<Rgba32>> images)
    {
        var palette = new List<RGBA>();
        var uniqueColors = new HashSet<RGBA>();

        foreach (var image in images)
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    var color = new RGBA { R = pixel.R, G = pixel.G, B = pixel.B, A = pixel.A };
                    if (uniqueColors.Add(color) && palette.Count < 256)
                    {
                        palette.Add(color);
                    }
                }
            }
        }

        while (palette.Count < 256)
        {
            palette.Add(new RGBA { R = 0, G = 0, B = 0, A = 255 });
        }

        return palette;
    }

    private byte[] ConvertFromBitmap(Image<Rgba32> image, bool compressed, List<RGBA> palette)
    {
        var imageData = new List<byte>();

        if (compressed)
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    var paletteIndex = palette.FindIndex(p => p.R == pixel.R && p.G == pixel.G && p.B == pixel.B && p.A == pixel.A);
                    imageData.Add((byte)paletteIndex);
                }
            }
        }
        else
        {
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    var paletteIndex = palette.FindIndex(p => p.R == pixel.R && p.G == pixel.G && p.B == pixel.B && p.A == pixel.A);
                    imageData.Add((byte)paletteIndex);
                }
            }
        }

        return imageData.ToArray();
    }
}