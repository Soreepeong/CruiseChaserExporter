using System.Collections.Immutable;

namespace CruiseChaserExporter.HavokCodec.HavokTagfile.Value;

public class ValueInt : IValue {
    public readonly int Value;

    public ValueInt(int value) {
        Value = value;
    }

    public override string ToString() => $"{Value}";

    public static implicit operator int(ValueInt d) => d.Value;

    internal static ValueInt Read(Parser parser) => new(parser.ReadInt());

    internal static ValueArray ReadVector(Parser parser, int count) {
        var unknown = parser.ReadInt();
        if (unknown != 4)
            throw new InvalidDataException();

        return new(Enumerable.Range(0, count).Select(_ => (IValue?) Read(parser)).ToImmutableList());
    }
}
