using System.Runtime.InteropServices;
using System.Text;
using CruiseChaserExporter.Util;
using Lumina.Data;

namespace CruiseChaserExporter.XivStruct;

public class PapFile : FileResource {
    public PapHeader Header;
    public List<PapAnimation> Animations = null!;
    public byte[] HavokData = null!;
    public byte[] Parameters = null!;

    public override void LoadFile() {
        Header = new(Reader);
        if (Header.Magic != 0x20706170)
            throw new InvalidDataException();
        Animations = Enumerable.Range(0, Header.AnimationCount).Select(_ => new PapAnimation(Reader)).ToList();
        
        HavokData = Data[Header.HavokDataOffset..Header.ParametersOffset];
        Parameters = Data[Header.ParametersOffset..];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PapHeader {
        public uint Magic;
        public short Unknown1;
        public short Unknown2;
        public short AnimationCount;
        public short Unknown3;
        public short Unknown4;
        public short Unknown5;
        public short Unknown6;
        public int HavokDataOffset;
        public int ParametersOffset;

        public PapHeader(BinaryReader r) {
            r.ReadInto(out Magic);
            r.ReadInto(out Unknown1);
            r.ReadInto(out Unknown2);
            r.ReadInto(out AnimationCount);
            r.ReadInto(out Unknown3);
            r.ReadInto(out Unknown4);
            r.ReadInto(out Unknown5);
            r.ReadInto(out Unknown6);
            r.ReadInto(out HavokDataOffset);
            r.ReadInto(out ParametersOffset);
        }
    }

    public class PapAnimation {
        public string Name;
        public short Unknown20;
        public int Index;
        public short Unknown26;

        public PapAnimation(BinaryReader r) {
            var nameBytes = r.ReadBytes(0x20);
            Name = Encoding.UTF8.GetString(nameBytes, 0, nameBytes.TakeWhile(x => x != 0).Count());
            r.ReadInto(out Unknown20);
            r.ReadInto(out Index);
            r.ReadInto(out Unknown26);
        }
    }
}
