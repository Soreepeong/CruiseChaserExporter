using System.Collections.Immutable;

namespace CruiseChaserExporter.HkTagfile.HkValue;

public class HkValueString : IHkValue {
    public readonly string Value;

    public HkValueString(string value) {
        Value = value;
    }

    public override string ToString() => Value;

    public static implicit operator string(HkValueString d) => d.Value;

    internal static HkValueString? Read(TagfileParser tagfileParser) {
        var s = tagfileParser.ReadStringNullable();
        return s == null ? null : new HkValueString(s);
    }

    internal static HkValueVector ReadVector(TagfileParser tagfileParser, int count)
        => new(Enumerable.Range(0, count).Select(_ => (IHkValue?) Read(tagfileParser)).ToImmutableList());
}
