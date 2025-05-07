using ii.LightSettlement.Model;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

public class BtsReader
{
    public List<RGBA> GetPalette(string fileName)
    {
        using var br = new BinaryReader(File.OpenRead(fileName));
        _ = br.ReadInt16();
        _ = br.ReadInt16(); // always 0
        var frameCount = br.ReadInt16();
        _ = br.ReadInt16(); // always 0

        var palette = new List<RGBA>();
        for (var i = 0; i < 256; i++)
        {
            palette.Add(new RGBA
            {
                R = (byte)(br.ReadByte() * 4 + 3),
                G = (byte)(br.ReadByte() * 4 + 3),
                B = (byte)(br.ReadByte() * 4 + 3),
                A = 255
            });
        }

        return palette;
    }

    public List<Image<Rgba32>> Process(string fileName)
    {
        const int FrameDataOffset = 776; // 8 byte header followed by 256 palette (3 bytes per entry)
        const int FrameLength = 1028;    // 32x32, 4 byte header
        const int FrameWidth = 32;
        const int FrameHeight = 32;

        using var br = new BinaryReader(File.OpenRead(fileName));
        _ = br.ReadInt16();
        _ = br.ReadInt16(); // always 0
        var frameCount = br.ReadInt16();
        _ = br.ReadInt16(); // always 0

        var palette = new List<RGBA>();
        for (var i = 0; i < 256; i++)
        {
            palette.Add(new RGBA
            {
                R = (byte)(br.ReadByte() * 4 + 3),
                G = (byte)(br.ReadByte() * 4 + 3),
                B = (byte)(br.ReadByte() * 4 + 3),
                A = 255
            });
        }

        var result = new List<Image<Rgba32>>();
        for (var i = 0; i < frameCount; i++)
        {
            var start = FrameDataOffset + (FrameLength * i);
            var bytes = new List<byte>();

            _  = br.ReadInt32(); // usu. 300+

            var byteIndex = start + 4;
            while (byteIndex < (start + FrameLength))
            {
                var dataByte = br.ReadByte();
                var rgb = palette[dataByte];

                bytes.Add(rgb.R);
                bytes.Add(rgb.G);
                bytes.Add(rgb.B);
                bytes.Add(255);
                byteIndex++;
            }

            var image = new Image<Rgba32>(FrameWidth, FrameHeight);
            for (var y = 0; y < FrameWidth; y++)
            {
                for (var x = 0; x < FrameWidth; x++)
                {
                    var index = (y * FrameHeight + x) * 4;
                    var color = new Rgba32(bytes[index], bytes[index + 1], bytes[index + 2], bytes[index + 3]);
                    image[x, y] = color;
                }
            }

            result.Add(image);
        }

        return result;
    }

    public void Write(string fileName, List<Image<Rgba32>> images, List<RGBA> palette)
    {
        const int FrameWidth = 32;
        const int FrameHeight = 32;

        if (palette.Count != 256)
        {
            throw new ArgumentException("Palette must contain 256 colors.");
        }

        using var bw = new BinaryWriter(File.OpenWrite(fileName));

        // Write header
        bw.Write((short)0); // Placeholder for header value
        bw.Write((short)0); // Always 0
        bw.Write((short)images.Count); // Frame count
        bw.Write((short)0); // Always 0

        // Generate and write palette
        //for (var i = 0; i < 256; i++)
        //{
        //    palette.Add(new RGBA
        //    {
        //        R = (byte)(i * 4 + 3),
        //        G = (byte)(i * 4 + 3),
        //        B = (byte)(i * 4 + 3),
        //        A = 255
        //    });
        //}

        foreach (var color in palette)
        {
            bw.Write((byte)((color.R - 3) / 4));
            bw.Write((byte)((color.G - 3) / 4));
            bw.Write((byte)((color.B - 3) / 4));
        }

        // Write frames
        foreach (var image in images)
        {
            if (image.Width != FrameWidth || image.Height != FrameHeight)
            {
                throw new ArgumentException($"All images must be {FrameWidth}x{FrameHeight}.");
            }

            bw.Write(300); // Placeholder for frame header value

            for (var y = 0; y < FrameHeight; y++)
            {
                for (var x = 0; x < FrameWidth; x++)
                {
                    var pixel = image[x, y];
                    var closestColorIndex = palette.FindIndex(p =>
                        p.R == pixel.R && p.G == pixel.G && p.B == pixel.B);

                    if (closestColorIndex == -1)
                    {
                        throw new InvalidOperationException("Image contains colors not in the palette.");
                    }

                    bw.Write((byte)closestColorIndex);
                }
            }
        }
    }
}