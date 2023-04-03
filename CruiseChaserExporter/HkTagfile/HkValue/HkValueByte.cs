using System.Collections.Immutable;

namespace CruiseChaserExporter.HkTagfile.HkValue;

public class HkValueByte : IHkValue {
    public readonly byte Value;

    public HkValueByte(byte value) {
        Value = value;
    }

    public override string ToString() => $"{Value}";

    public static implicit operator byte(HkValueByte d) => d.Value;

    internal static HkValueByte Read(TagfileParser tagfileParser) => new(tagfileParser.ReadByte());

    internal static HkValueVector ReadVector(TagfileParser tagfileParser, int count) {
        // throw new NotSupportedException();
        return new(Enumerable.Range(0, count).Select(_ => (IHkValue?) Read(tagfileParser)).ToImmutableList());
    }
}
