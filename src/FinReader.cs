using System.Text;

public class FinReader
{
    public void Process(string fileName)
    {
        using var br = new BinaryReader(File.OpenRead(fileName));
        var signature = br.ReadInt16(); // 0x1d00
        var unknownBlockCount = br.ReadInt16();
        var animationSequenceCount = br.ReadInt16();
        var sprCount = br.ReadInt16();

        for (int i = 0; i < sprCount; i++)
        {
            var sprFilenameBytes = br.ReadBytes(8);
            var sprFilename = Encoding.ASCII.GetString(sprFilenameBytes);
        }

        // 20 bytes
        for (int i = 0; i < animationSequenceCount; i++)
        {
            var sequenceDescriptionBytes = br.ReadBytes(16);
            var sequenceName = Encoding.ASCII.GetString(sequenceDescriptionBytes);
            var startFrame = br.ReadInt16();
            var endFrame = br.ReadInt16();
        }

        for (int i = 0; i < unknownBlockCount; i++)
        {
            _ = br.ReadInt16();
            _ = br.ReadInt16();

            for (int j = 0; j < 8; j++)
            {
                var nameBytes = br.ReadBytes(16);
                var name = Encoding.ASCII.GetString(nameBytes);
                _ = br.ReadInt32();
            }
        }

        var cnt = (br.BaseStream.Length - br.BaseStream.Position) / 22;

        // 22 bytes
        for (int i = 0; i < cnt; i++)
        {            
            var sprFilenameBytes = br.ReadBytes(8);
            var sprFilename = Encoding.ASCII.GetString(sprFilenameBytes);
            var frameNumber = br.ReadInt16();
            _ = br.ReadInt16();
            _ = br.ReadInt16();
            _ = br.ReadInt16();
            _ = br.ReadInt16();
            _ = br.ReadInt16();
            _ = br.ReadInt16();
        }
    }   
}