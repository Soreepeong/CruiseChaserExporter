using System.Collections;

namespace CruiseChaserExporter.HkTagfile;

public class HkBitfield : IEnumerable<bool> {
    private readonly int _length;
    private readonly byte[] _bitfield;

    public HkBitfield(int length, byte[] bitfield) {
        _length = length;
        _bitfield = bitfield;
    }

    public bool this[int index] => 0 <= index && index <= _length
        ? 0 != (_bitfield[index >> 3] & (1 << (index & 7)))
        : throw new ArgumentOutOfRangeException(nameof(index), index, null);

    internal static HkBitfield Read(TagfileParser tagfileParser, int length) {
        return new(length, tagfileParser.ReadBytes((length + 7) / 8));
    }

    public IEnumerator<bool> GetEnumerator() {
        for (var i = 0; i < _length; i++)
            yield return 0 != (_bitfield[i >> 3] & (1 << (i & 7)));
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}