using System.Text;

public class FinProcessor
{
    public Fin Read(string fileName)
    {
        var fin = new Fin();

        using var br = new BinaryReader(File.OpenRead(fileName));
        var signature = br.ReadInt16(); // 0x1d00
        var unknownBlockCount = br.ReadInt16();
        var animationSequenceCount = br.ReadInt16();
        var sprCount = br.ReadInt16();

        // Spr filenames - 8 bytes
        for (int i = 0; i < sprCount; i++)
        {
            var sprFilenameBytes = br.ReadBytes(8);
            var sprFilename = Encoding.ASCII.GetString(sprFilenameBytes).Replace("\0", string.Empty);
            fin.SprFilenames.Add(sprFilename);
        }

        // Animation sequences - 20 bytes
        for (int i = 0; i < animationSequenceCount; i++)
        {
            var sequenceDescriptionBytes = br.ReadBytes(16);
            var sequenceName = Encoding.ASCII.GetString(sequenceDescriptionBytes).Replace("\0", string.Empty);
            var startFrame = br.ReadInt16();
            var endFrame = br.ReadInt16();
            fin.AnimationSequencesInfos.Add(new AnimationSequenceInfo
            {
                SequenceNames = sequenceName,
                StartFrame = startFrame,
                EndFrame = endFrame
            });
        }

        for (int i = 0; i < unknownBlockCount; i++)
        {
            _ = br.ReadInt16();
            _ = br.ReadInt16();

            for (int j = 0; j < 8; j++)
            {
                var nameBytes = br.ReadBytes(16);
                var name = Encoding.ASCII.GetString(nameBytes).Replace("\0", string.Empty);
                _ = br.ReadInt32();
            }
        }

        var cnt = (br.BaseStream.Length - br.BaseStream.Position) / 22;

        // 22 bytes
        for (int i = 0; i < cnt; i++)
        {            
            var sprFilenameBytes = br.ReadBytes(8);
            var sprFilename = Encoding.ASCII.GetString(sprFilenameBytes).Replace("\0", string.Empty);
            var frameNumber = br.ReadInt16();
            var unknown1 = br.ReadInt16();
            var unknown2 = br.ReadInt16();
            var unknown3 = br.ReadInt16();
            var unknown4 = br.ReadInt16();
            var unknown5 = br.ReadInt16();
            var unknown6 = br.ReadInt16();

            fin.FrameInfos.Add(new FrameInfo
            {
                Filename = sprFilename,
                FrameNumber = frameNumber,
                Unknown1 = unknown1,
                Unknown2 = unknown2,
                Unknown3 = unknown3,
                Unknown4 = unknown4,
                Unknown5 = unknown5,
                Unknown6 = unknown6,
            });
        }

        return fin;
    }

    public void Write(string fileName, Fin fin)
    {
        var unknownBlockCount = 1;

        using var bw = new BinaryWriter(File.Open(fileName, FileMode.Create));
        bw.Write((short)0x1d00);        
        bw.Write((short)unknownBlockCount);
        bw.Write((short)fin.AnimationSequencesInfos.Count);
        bw.Write((short)fin.SprFilenames.Count);

        // Spr filenames - 8 bytes
        foreach (var sprFilename in fin.SprFilenames)
        {
            var sprFilenameBytes = Encoding.ASCII.GetBytes(sprFilename.PadRight(8, '\0'));
            bw.Write(sprFilenameBytes);
        }

        // Animation sequences - 20 bytes
        foreach (var sequence in fin.AnimationSequencesInfos)
        {
            var sequenceNameBytes = Encoding.ASCII.GetBytes(sequence.SequenceNames.PadRight(16, '\0'));
            bw.Write(sequenceNameBytes);
            bw.Write((short)sequence.StartFrame);
            bw.Write((short)sequence.EndFrame);
        }

        // Unknown blocks
        for (int i = 0; i < unknownBlockCount; i++)
        {
            bw.Write((short)0);
            bw.Write((short)0);
            for (int j = 0; j < 8; j++)
            {
                var nameBytes = Encoding.ASCII.GetBytes("".PadRight(16, '\0'));
                bw.Write(nameBytes);
                bw.Write(0);
            }
        }

        // Frame infos - 232 bytes
        foreach (var frame in fin.FrameInfos)
        {
            var sprFilenameBytes = Encoding.ASCII.GetBytes(frame.Filename.PadRight(8, '\0'));
            bw.Write(sprFilenameBytes);
            bw.Write((short)frame.FrameNumber);
            bw.Write((short)frame.Unknown1);
            bw.Write((short)frame.Unknown2);
            bw.Write((short)frame.Unknown3);
            bw.Write((short)frame.Unknown4);
            bw.Write((short)frame.Unknown5);
            bw.Write((short)frame.Unknown6);
        }
    }

    public class Fin
    {
        public List<string> SprFilenames { get; set; } = [];
        public List<AnimationSequenceInfo> AnimationSequencesInfos { get; set; } = [];
        public List<FrameInfo> FrameInfos { get; set; } = [];
    }

    public class AnimationSequenceInfo
    {
        public string SequenceNames { get; set; } = string.Empty;
        public int StartFrame { get; set; }
        public int EndFrame { get; set; }
    }

    public class FrameInfo
    {
        public string Filename { get; set; } = string.Empty;
        public int FrameNumber { get; set; }
        public int Unknown1 { get; set; }
        public int Unknown2 { get; set; }
        public int Unknown3 { get; set; }
        public int Unknown4 { get; set; }
        public int Unknown5 { get; set; }
        public int Unknown6 { get; set; }
    }
}