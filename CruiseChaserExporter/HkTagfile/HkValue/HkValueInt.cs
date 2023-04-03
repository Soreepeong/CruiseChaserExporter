using System.Collections.Immutable;

namespace CruiseChaserExporter.HkTagfile.HkValue;

public class HkValueInt : IHkValue {
    public readonly int Value;

    public HkValueInt(int value) {
        Value = value;
    }

    public override string ToString() => $"{Value}";

    public static implicit operator int(HkValueInt d) => d.Value;

    internal static HkValueInt Read(TagfileParser tagfileParser) => new(tagfileParser.ReadInt());

    internal static HkValueVector ReadVector(TagfileParser tagfileParser, int count) {
        var unknown = tagfileParser.ReadInt();
        if (unknown != 4)
            throw new InvalidDataException();

        return new(Enumerable.Range(0, count).Select(_ => (IHkValue?) Read(tagfileParser)).ToImmutableList());
    }
}
