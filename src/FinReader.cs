using System.Text;

public class FinReader
{
    public void Process(string fileName)
    {
        var c2 = 0;

        if (fileName.ToLower().EndsWith("art.fin"))
        {
            c2 = 5;
        }

        if (fileName.ToLower().EndsWith("abuildng.fin"))
        {
            c2 = 25;
        }

        if (fileName.ToLower().EndsWith("aird.fin"))
        {
            c2 = 144;
        }


        using var br = new BinaryReader(File.OpenRead(fileName));
        var signature = br.ReadInt16(); // 0x1d00
        var unknownBlockCount = br.ReadInt16();
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
            var a = br.ReadInt16();
            var b = br.ReadInt16();
        }

        for (int i = 0; i < unknownBlockCount; i++)
        {
            _ = br.ReadInt16();
            _ = br.ReadInt16();

            for (int j = 0; j < 8; j++)
            {
                var sprFilenameBytes = br.ReadBytes(16);
                var sprFilename = Encoding.ASCII.GetString(sprFilenameBytes);
                var x = br.ReadInt32();
            }
        }

        // 22 bytes
        for (int i = 0; i < c2; i++)
        {            
            var sprFilenameBytes = br.ReadBytes(8);
            var sprFilename = Encoding.ASCII.GetString(sprFilenameBytes);
            var frameNumber = br.ReadInt16();
            var c = br.ReadInt16();
            var d = br.ReadInt16();
            var f = br.ReadInt16();
            var e = br.ReadInt16();
            var g = br.ReadInt16();
            var a = br.ReadInt16();
        }
    }   
}