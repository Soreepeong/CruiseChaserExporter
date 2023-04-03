using System.Runtime.InteropServices;
using CruiseChaserExporter.Util;
using Lumina.Data;

namespace CruiseChaserExporter.XivStruct;

public class SklbFile : FileResource {
    public SklbHeader Header;
    public int Unknown0;
    public int HavokOffset;
    public byte[] HavokData = null!;

    public override void LoadFile() {
        Header = new(Reader);
        if (Header.Magic != 0x736B6C62)
            throw new InvalidDataException();

        switch (Header.Version) {
            case SklbFormat.K0021:
                var k0021 = new Sklb0021(Reader);
                Unknown0 = k0021.Unknown0;
                HavokOffset = k0021.HavokOffset;
                break;
            case SklbFormat.K0031:
                var k0031 = new Sklb0031(Reader);
                Unknown0 = (int) k0031.Unknown0;
                HavokOffset = (int) k0031.HavokOffset;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        HavokData = Data[HavokOffset..];
    }

    public enum SklbFormat : uint {
        K0021 = 0x31323030u,
        K0031 = 0x31333030u,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SklbHeader {
        public uint Magic;
        public SklbFormat Version;

        public SklbHeader(BinaryReader r) {
            r.ReadInto(out Magic);
            r.ReadInto(out Version);
        }
    }

    public struct Sklb0021 {
        public ushort Unknown0;
        public ushort HavokOffset;

        public Sklb0021(BinaryReader r) {
            r.ReadInto(out Unknown0);
            r.ReadInto(out HavokOffset);
        }
    }

    public struct Sklb0031 {
        public uint Unknown0;
        public uint HavokOffset;

        public Sklb0031(BinaryReader r) {
            r.ReadInto(out Unknown0);
            r.ReadInto(out HavokOffset);
        }
    }
}
