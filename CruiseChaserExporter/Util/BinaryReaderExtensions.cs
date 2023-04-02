using System.Numerics;
using System.Runtime.InteropServices;

namespace CruiseChaserExporter.Util;

public static class BinaryReaderExtensions {
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

    public static void ReadInto<T>(this BinaryReader reader, out T value) where T : unmanaged, Enum
        => value = reader.ReadEnum<T>();

    public static void AlignTo(this BinaryReader reader, int unit) =>
        reader.BaseStream.Position = (reader.BaseStream.Position + unit - 1) / unit * unit;

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
        const int delta = 0x801;
        const float fractal = 0.000345436f;

        var n = (ulong)reader.ReadUInt32();
        n |= (ulong)reader.ReadByte() << 32;

        var x = (int)((n >> 0) & 0xFFF);
        var y = (int)((n >> 12) & 0xFFF);
        var z = (int)((n >> 24) & 0xFFF);
        var shift = (int)((n >> 36) & 0x3);
        var invert = 0 != ((n >> 38) & 0x1);
        var invalid = 0 != ((n >> 39) & 0x1);

        if (invalid)
            throw new InvalidDataException();

        var tmp = new[] {
            (x - delta) * fractal,
            (y - delta) * fractal,
            (z - delta) * fractal,
            0f,
        };
        tmp[3] = MathF.Sqrt(1f - tmp[0] * tmp[0] - tmp[1] * tmp[1] - tmp[2] * tmp[2]) * (invert ? -1 : 1);

        for (var i = 0; i < 3 - shift; ++i)
            (tmp[3 - i], tmp[2 - i]) = (tmp[2 - i], tmp[3 - i]);
        
        return new(tmp[0], tmp[1], tmp[2], tmp[3]);
    }
}