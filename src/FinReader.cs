using System.Text;

public class FinReader
{
    public void Process(string fileName)
    {
        using (var br = new BinaryReader(File.OpenRead(fileName)))
        {
            var signature = br.ReadInt16(); // 0x1d00
            var count2 = br.ReadInt16();
            var sprCount = br.ReadInt16();
            var count4 = br.ReadInt16();

            for (int i = 0; i < sprCount; i++)
            {
                var sprFilenameBytes = br.ReadBytes(8);
                var sprFilename = Encoding.ASCII.GetString(sprFilenameBytes);
            }
        }
    }
}