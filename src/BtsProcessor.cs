using ii.LightSettlement.Model;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

public class BtsProcessor
{
    private Dictionary<int, int> _fidToFrameIndex = new Dictionary<int, int>();
    private List<Image<Rgba32>> _frames = new List<Image<Rgba32>>();

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

    public List<Image<Rgba32>> Read(string fileName)
    {
        const int FrameDataOffset = 776; // 8 byte header followed by 256 palette (3 bytes per entry)
        const int FrameLength = 1028;    // 32x32, 4 byte header
        const int FrameWidth = 32;
        const int FrameHeight = 32;

        using var br = new BinaryReader(File.OpenRead(fileName));
        _ = br.ReadInt16();
        _ = br.ReadInt16(); // Always 0
        var frameCount = br.ReadInt16();
        _ = br.ReadInt16(); // Always 0

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
        _fidToFrameIndex.Clear();
        _frames.Clear();
        
        for (var i = 0; i < frameCount; i++)
        {
            var start = FrameDataOffset + (FrameLength * i);
            br.BaseStream.Seek(start, SeekOrigin.Begin);
            
            // Read the Frame ID (FID) from the 4-byte header
            var fid = br.ReadInt32();
            _fidToFrameIndex[fid] = i; // Map FID to frame array index

            var image = new Image<Rgba32>(FrameWidth, FrameHeight);
            for (var y = 0; y < FrameHeight; y++)
            {
                for (var x = 0; x < FrameWidth; x++)
                {
                    var dataByte = br.ReadByte();
                    if (dataByte >= palette.Count)
                    {
                        dataByte = 0;
                    }
                    var rgb = palette[dataByte];

                    // Check for transparency (magenta: RGB 255, 3, 255)
                    byte alpha = 255;
                    if (rgb.R == 255 && rgb.G == 3 && rgb.B == 255)
                    {
                        alpha = 0; // Make transparent
                    }

                    image[x, y] = new Rgba32(rgb.R, rgb.G, rgb.B, alpha);
                }
            }

            result.Add(image);
            _frames.Add(image);
        }

        return result;
    }

    public Image<Rgba32>? GetFrameByFID(int fid)
    {
        if (_fidToFrameIndex.TryGetValue(fid, out int frameIndex))
        {
            return _frames[frameIndex];
        }
        return null;
    }

    public void Write(string fileName, List<(Image<Rgba32> image, int infoByte)> images, List<RGBA> palette)
    {
        const int FrameWidth = 32;
        const int FrameHeight = 32;

        if (palette.Count != 256)
        {
            throw new ArgumentException("Palette must contain 256 colors.");
        }

        using var bw = new BinaryWriter(File.Open(fileName, FileMode.Create));

        // Write header
        bw.Write((short)0);
        bw.Write((short)0); // Always 0
        bw.Write((short)images.Count); // Frame count
        bw.Write((short)0); // Always 0

        foreach (var color in palette)
        {
            bw.Write((byte)((color.R - 3) / 4));
            bw.Write((byte)((color.G - 3) / 4));
            bw.Write((byte)((color.B - 3) / 4));
        }

        // Write frames with 4-byte headers
        foreach (var image in images)
        {
            if (image.image.Width != FrameWidth || image.image.Height != FrameHeight)
            {
                throw new ArgumentException($"All images must be {FrameWidth}x{FrameHeight}.");
            }

            bw.Write(image.infoByte); // 4-byte header per frame (FID)

            for (var y = 0; y < FrameHeight; y++)
            {
                for (var x = 0; x < FrameWidth; x++)
                {
                    var pixel = image.image[x, y];
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