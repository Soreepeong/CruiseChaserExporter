using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace CruiseChaserExporter.Util;

public static class BinaryReaderExtensions {
    private const float QuaternionComponentRange = 0.707106781186f; // 1 / sqrt(2)

    public static unsafe T ReadEnum<T>(this BinaryReader reader) where T : unmanaged, Enum {
        switch (Marshal.SizeOf(Enum.GetUnderlyingType(typeof(T)))) {
            case 1:
                var b1 = reader.ReadByte();
                return *(T*) &b1;
            case 2:
                var b2 = reader.ReadUInt16();
                return *(T*) &b2;
            case 4:
                var b4 = reader.ReadUInt32();
                return *(T*) &b4;
            case 8:
                var b8 = reader.ReadUInt64();
                return *(T*) &b8;
            default:
                throw new ArgumentException("Enum is not of size 1, 2, 4, or 8.", nameof(T), null);
        }
    }

    public static T SeekThen<T>(this T reader, long offset, SeekOrigin origin) where T : BinaryReader {
        reader.BaseStream.Seek(offset, origin);
        return reader;
    }

    public static string ReadCString(this BinaryReader reader) {
        var len = 0;
        while (reader.ReadByte() != 0)
            len++;

        reader.BaseStream.Seek(0 - len - 1, SeekOrigin.Current);

        var bytes = reader.ReadBytes(len + 1);
        return Encoding.UTF8.GetString(bytes, 0, len);
    }

    public static void ReadInto(this BinaryReader reader, out byte value) => value = reader.ReadByte();
    public static void ReadInto(this BinaryReader reader, out sbyte value) => value = reader.ReadSByte();
    public static void ReadInto(this BinaryReader reader, out ushort value) => value = reader.ReadUInt16();
    public static void ReadInto(this BinaryReader reader, out short value) => value = reader.ReadInt16();
    public static void ReadInto(this BinaryReader reader, out uint value) => value = reader.ReadUInt32();
    public static void ReadInto(this BinaryReader reader, out int value) => value = reader.ReadInt32();
    public static void ReadInto(this BinaryReader reader, out ulong value) => value = reader.ReadUInt64();
    public static void ReadInto(this BinaryReader reader, out long value) => value = reader.ReadInt64();
    public static void ReadInto(this BinaryReader reader, out float value) => value = reader.ReadSingle();
    public static void ReadInto(this BinaryReader reader, out double value) => value = reader.ReadDouble();

    public static void ReadInto(this BinaryReader reader, out Quaternion value) =>
        value = reader.ReadSingleQuaternion();

    public static void ReadInto(this BinaryReader reader, out Vector3 value) => value = reader.ReadSingleVector3();

    public static void ReadInto<T>(this BinaryReader reader, out T value) where T : unmanaged, Enum
        => value = reader.ReadEnum<T>();

    public static void AlignTo(this BinaryReader reader, int unit) =>
        reader.BaseStream.Position = (reader.BaseStream.Position + unit - 1) / unit * unit;

    public static Vector3 ReadSingleVector3(this BinaryReader reader) =>
        new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    public static Quaternion ReadSingleQuaternion(this BinaryReader reader) =>
        new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    public static Quaternion ReadHk32BitQuaternion(this BinaryReader reader) {
        const float piDiv2 = MathF.PI / 2;
        const float piDiv4 = MathF.PI / 4;
        const float phiFrac = piDiv2 / 511f;

        var cVal = reader.ReadUInt32();

        var phiTheta = (float) (cVal & 0x3FFFF);

        var r = 1f * ((cVal >> 18) & 0x3FF) / 0x3FF;
        r = 1.0f - r * r;

        var phi = MathF.Floor(MathF.Sqrt(phiTheta));
        var theta = 0f;

        if (phi != 0f) {
            theta = piDiv4 * (phiTheta - phi * phi) / phi;
            phi = phiFrac * phi;
        }

        var magnitude = MathF.Sqrt(1.0f - r * r);
        var (sPhi, cPhi) = MathF.SinCos(phi);
        var (sTheta, cTheta) = MathF.SinCos(theta);

        return new(
            sPhi * cTheta * magnitude * (0 == (cVal & 0x10000000) ? 1 : -1),
            sPhi * sTheta * magnitude * (0 == (cVal & 0x20000000) ? 1 : -1),
            cPhi * magnitude * (0 == (cVal & 0x40000000) ? 1 : -1),
            r * (0 == (cVal & 0x80000000) ? 1 : -1));
    }

    public static Quaternion ReadHk40BitQuaternion(this BinaryReader reader) {
        /*
         * 40 bit Quaternion structure
         * - 12 bit x signed integer
         * - 12 bit y signed integer
         * - 12 bit z signed integer
         * - 2 bit shift
         * - 1 bit invert sign
         * - 1 bit unused?
         */
        const int mask = 0xFFF;

        var n = (ulong) reader.ReadUInt32();
        n |= (ulong) reader.ReadByte() << 32;

        var x = (int) (n >> 0);
        var y = (int) (n >> 12);
        var z = (int) (n >> 24);
        var shift = (int) ((n >> 36) & 0x3);
        var invert = 0 != ((n >> 38) & 0x1);
        var invalid = 0 != ((n >> 39) & 0x1);

        if (invalid)
            throw new InvalidDataException();

        var tmp = new[] {
            ((x & mask) - (mask >> 1)) * QuaternionComponentRange / (mask >> 1),
            ((y & mask) - (mask >> 1)) * QuaternionComponentRange / (mask >> 1),
            ((z & mask) - (mask >> 1)) * QuaternionComponentRange / (mask >> 1),
            0f,
        };
        tmp[3] = MathF.Sqrt(1f - tmp[0] * tmp[0] - tmp[1] * tmp[1] - tmp[2] * tmp[2]) * (invert ? -1 : 1);

        for (var i = 0; i < 3 - shift; ++i)
            (tmp[3 - i], tmp[2 - i]) = (tmp[2 - i], tmp[3 - i]);

        return new(tmp[0], tmp[1], tmp[2], tmp[3]);
    }

    public static Quaternion ReadHk48BitQuaternion(this BinaryReader reader) {
        const int mask = 0x7FFF;

        var x = reader.ReadUInt16();
        var y = reader.ReadUInt16();
        var z = reader.ReadUInt16();
        var shift = ((y >> 14) & 2) | (x >> 15);
        var invert = 0 != (z & 0x8000);

        var tmp = new[] {
            ((x & mask) - (mask >> 1)) * QuaternionComponentRange / (mask >> 1),
            ((y & mask) - (mask >> 1)) * QuaternionComponentRange / (mask >> 1),
            ((z & mask) - (mask >> 1)) * QuaternionComponentRange / (mask >> 1),
            0f,
        };
        tmp[3] = MathF.Sqrt(1f - tmp[0] * tmp[0] - tmp[1] * tmp[1] - tmp[2] * tmp[2]) * (invert ? -1 : 1);
        if (tmp[3] != tmp[3])
            throw new InvalidOperationException();

        for (var i = 0; i < 3 - shift; ++i)
            (tmp[3 - i], tmp[2 - i]) = (tmp[2 - i], tmp[3 - i]);

        return new(tmp[0], tmp[1], tmp[2], tmp[3]);
    }
}
