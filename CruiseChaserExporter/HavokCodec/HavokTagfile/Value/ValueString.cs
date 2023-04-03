using System.Collections.Immutable;

namespace CruiseChaserExporter.HavokCodec.HavokTagfile.Value;

public class ValueString : IValue {
    public readonly string Value;

    public ValueString(string value) {
        Value = value;
    }

    public override string ToString() => Value;

    public static implicit operator string(ValueString d) => d.Value;

    internal static ValueString? Read(Parser parser) {
        var s = parser.ReadStringNullable();
        return s == null ? null : new ValueString(s);
    }

    internal static ValueArray ReadVector(Parser parser, int count)
        => new(Enumerable.Range(0, count).Select(_ => (IValue?) Read(parser)).ToImmutableList());
}
