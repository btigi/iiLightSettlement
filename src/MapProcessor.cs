using ii.LightSettlement.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class MapProcessor
{
    public Map Read(string fileName)
    {
        using var br = new BinaryReader(File.OpenRead(fileName));

        var width = br.ReadInt32();
        var height = br.ReadInt32();

        var map = new Map
        {
            Width = width,
            Height = height,
            Tiles = [],
            Flags = []
        };

        var noTiles = width * height;

        // Read main and overlay tiles (4 bytes per tile: 2 bytes main, 2 bytes overlay)
        for (var i = 0; i < noTiles; i++)
        {
            var mainTile = br.ReadInt16();
            var overlayTile = br.ReadInt16();

            map.Tiles.Add(new MapTile
            {
                MainTile = mainTile,
                OverlayTile = overlayTile
            });
        }

        // Read flag bytes
        for (var t = 0; t < noTiles; t++)
        {
            var flagByte = br.ReadInt16();

            map.Flags.Add(new MapTileFlags
            {
                X1 = (flagByte >> 1) & 1,
                X2 = (flagByte >> 2) & 1,
                FlipMainTile = (flagByte >> 5) & 1,
                FlipOverlayTile = (flagByte >> 6) & 1,
                BlocksView = (flagByte >> 7) & 1,
                X8 = (flagByte >> 8) & 1,
                RawFlagByte = flagByte
            });
        }

        return map;
    }

    public List<RGBA> CreateCollisionArea(Map map, out int width, out int height)
    {
        width = (int)(map.Width * 32);
        height = (int)(map.Height * 32);

        var pixels = new List<RGBA>(width * height);

        // Initialize all pixels as transparent
        for (int i = 0; i < width * height; i++)
        {
            pixels.Add(new RGBA { R = 0, G = 0, B = 0, A = 0 });
        }

        // Define colors for each collision type
        var x1Color = new RGBA { R = 255, G = 0, B = 0, A = 150 };      // Red - X1 collision
        var x2Color = new RGBA { R = 0, G = 0, B = 255, A = 150 };      // Blue - X2 collision  
        var x8Color = new RGBA { R = 0, G = 255, B = 0, A = 150 };      // Green - X8 collision
        var blocksViewColor = new RGBA { R = 255, G = 255, B = 0, A = 150 }; // Yellow - Blocks view

        var curX = 0;
        var curY = 0;

        for (var t = 0; t < map.Flags.Count; t++)
        {
            var flags = map.Flags[t];

            // Check if this tile has any collision flags
            if (flags.X1 == 1 || flags.X2 == 1 || flags.X8 == 1 || flags.BlocksView != 1)
            {
                // Fill a 32x32 rectangle, layering colors for multiple collision types
                for (var y = 0; y < 32; y++)
                {
                    for (var x = 0; x < 32; x++)
                    {
                        var pixelX = curX * 32 + x;
                        var pixelY = curY * 32 + y;
                        if (pixelX < width && pixelY < height)
                        {
                            var index = pixelY * width + pixelX;
                            var finalColor = new RGBA { R = 0, G = 0, B = 0, A = 0 };

                            // Layer colors for each collision type present
                            if (flags.X1 == 1)
                                finalColor = BlendColors(finalColor, x1Color);
                            if (flags.X2 == 1)
                                finalColor = BlendColors(finalColor, x2Color);
                            if (flags.X8 == 1)
                                finalColor = BlendColors(finalColor, x8Color);
                            if (flags.BlocksView != 1)
                                finalColor = BlendColors(finalColor, blocksViewColor);

                            pixels[index] = finalColor;
                        }
                    }
                }
            }

            curX++;
            if (curX >= map.Width)
            {
                curX = 0;
                curY++;
            }
        }

        return pixels;
    }

    private RGBA BlendColors(RGBA baseColor, RGBA overlayColor)
    {
        if (baseColor.A == 0)
            return overlayColor;

        // Additive blending with alpha
        var alpha = Math.Min(255, baseColor.A + overlayColor.A);
        var r = Math.Min(255, baseColor.R + overlayColor.R / 2);
        var g = Math.Min(255, baseColor.G + overlayColor.G / 2);
        var b = Math.Min(255, baseColor.B + overlayColor.B / 2);

        return new RGBA { R = (byte)r, G = (byte)g, B = (byte)b, A = (byte)alpha };
    }

    public List<RGBA> RenderMap(Map map, BtsProcessor btsProcessor, out int width, out int height)
    {
        width = (int)(map.Width * 32);
        height = (int)(map.Height * 32);

        var pixels = new List<RGBA>(width * height);

        // Initialize all pixels as transparent
        for (var i = 0; i < width * height; i++)
        {
            pixels.Add(new RGBA { R = 0, G = 0, B = 0, A = 0 });
        }

        var curX = 0;
        var curY = 0;

        for (var t = 0; t < map.Tiles.Count; t++)
        {
            var tile = map.Tiles[t];
            var flags = map.Flags[t];

            // Draw main tile using FID lookup
            if (tile.MainTile > 0)
            {
                var mainTileImage = btsProcessor.GetFrameByFID(tile.MainTile);
                if (mainTileImage != null)
                {
                    DrawTileToPixels(pixels, mainTileImage, curX, curY, width, height, flags.FlipMainTile == 1);
                }
            }

            // Draw overlay tile using FID lookup
            if (tile.OverlayTile > 0)
            {
                var overlayTileImage = btsProcessor.GetFrameByFID(tile.OverlayTile);
                if (overlayTileImage != null)
                {
                    DrawTileToPixels(pixels, overlayTileImage, curX, curY, width, height, flags.FlipOverlayTile == 1);
                }
            }

            curX++;
            if (curX >= map.Width)
            {
                curX = 0;
                curY++;
            }
        }

        return pixels;
    }

    private void DrawTileToPixels(List<RGBA> pixels, Image<Rgba32> tileImage, int tileX, int tileY, int canvasWidth, int canvasHeight, bool flip)
    {
        for (var y = 0; y < 32 && y < tileImage.Height; y++)
        {
            for (var x = 0; x < 32 && x < tileImage.Width; x++)
            {
                var sourceX = flip ? (31 - x) : x;
                var pixel = tileImage[sourceX, y];

                // Skip transparent pixels
                if (pixel.A == 0)
                    continue;

                var destX = tileX * 32 + x;
                var destY = tileY * 32 + y;

                if (destX < canvasWidth && destY < canvasHeight)
                {
                    var index = destY * canvasWidth + destX;
                    pixels[index] = new RGBA
                    {
                        R = pixel.R,
                        G = pixel.G,
                        B = pixel.B,
                        A = pixel.A
                    };
                }
            }
        }
    }

    public class Map
    {
        public long Width { get; set; }
        public long Height { get; set; }
        public List<MapTile> Tiles { get; set; } = [];
        public List<MapTileFlags> Flags { get; set; } = [];
    }

    public class MapTile
    {
        public int MainTile { get; set; }
        public int OverlayTile { get; set; }
    }

    public class MapTileFlags
    {
        public int X1 { get; set; }
        public int X2 { get; set; }
        public int FlipMainTile { get; set; }
        public int FlipOverlayTile { get; set; }
        public int BlocksView { get; set; }
        public int X8 { get; set; }
        public int RawFlagByte { get; set; }
    }
}