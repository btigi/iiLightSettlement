using System.Collections;
using System.Text;

public class FinReader
{
    public void Process(string fileName)
    {
        var c1 = 0;
        var c2 = 0;

        if (fileName.ToLower().EndsWith("art.fin"))
        {
            // incorrect - maybe these are not all the same type of block?
            c1 = 40;
            c2 = 10;
        }

        if (fileName.ToLower().EndsWith("abuildng.fin"))
        {
            c1 = 172;
            c2 = 25;
        }

        if (fileName.ToLower().EndsWith("aird.fin"))
        {
            c1 = 918;
            c2 = 144;
        }


        using var br = new BinaryReader(File.OpenRead(fileName));
        var signature = br.ReadInt16(); // 0x1d00
        var count2 = br.ReadInt16();
        var dataBlockCount = br.ReadInt16();
        var sprCount = br.ReadInt16();

        for (int i = 0; i < sprCount; i++)
        {
            var sprFilenameBytes = br.ReadBytes(8);
            var sprFilename = Encoding.ASCII.GetString(sprFilenameBytes);
        }

        // 20 bytes
        for (int i = 0; i < dataBlockCount; i++)
        {
            var sprFilenameBytes = br.ReadBytes(16);
            var sprFilename = Encoding.ASCII.GetString(sprFilenameBytes);
            var start = br.ReadInt16();
            var end = br.ReadInt16();
        }

        _ = br.ReadInt16();

        // 20 bytes
        for (int i = 0; i < c1; i++)
        {
            var x = br.ReadInt16();
            var sprFilenameBytes = br.ReadBytes(18);
            var sprFilename = Encoding.ASCII.GetString(sprFilenameBytes);
        }

        // 22 bytes
        for (int i = 0; i < c2; i++)
        {
            var a = br.ReadInt16();
            var sprFilenameBytes = br.ReadBytes(8);
            var sprFilename = Encoding.ASCII.GetString(sprFilenameBytes);
            var b = br.ReadInt16();
            var c = br.ReadInt16();
            var d = br.ReadInt16();
            var f = br.ReadInt16();
            var e = br.ReadInt16();
            var g = br.ReadInt16();
        }

        _ = br.ReadInt16();

    }   
}