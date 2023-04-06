using System.Runtime.InteropServices;
using System.Text;
using CruiseChaserExporter.Util;
using Lumina.Data;

namespace CruiseChaserExporter.XivStruct;

public class PapFile : FileResource {
    public PapHeader Header;
    public List<PapAnimation> Animations = null!;
    public byte[] HavokData = null!;
    public byte[] Timeline = null!;

    public override void LoadFile() {
        Header = new(Reader);
        if (Header.Magic != 0x20706170)
            throw new InvalidDataException();
        Animations = Enumerable.Range(0, Header.AnimationCount).Select(_ => new PapAnimation(Reader)).ToList();
        
        HavokData = Data[Header.HavokDataOffset..Header.TimelineOffset];
        Timeline = Data[Header.TimelineOffset..];
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PapHeader {
        public uint Magic;
        public uint Version;  // always 0x00020001?
        public short AnimationCount;
        public ushort ModelId;
        public PapTargetModelType ModelType;
        public int InfoOffset;
        public int HavokDataOffset;
        public int TimelineOffset;

        public PapHeader(BinaryReader r) {
            r.ReadInto(out Magic);
            r.ReadInto(out Version);
            r.ReadInto(out AnimationCount);
            r.ReadInto(out ModelId);
            r.ReadInto(out ModelType);
            r.ReadInto(out InfoOffset);
            r.ReadInto(out HavokDataOffset);
            r.ReadInto(out TimelineOffset);
        }
    }

    public enum PapTargetModelType : short {
        Invalid = -1,
        Human = 0,
        Monster = 1,
        DemiHuman = 2,
        Weapon = 3
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
