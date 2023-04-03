namespace CruiseChaserExporter.HavokCodec.HavokTagfile.Value;

public class ValueFloat : IValue {
    public readonly float Value;

    public ValueFloat(float value) {
        Value = value;
    }

    public override string ToString() => $"{Value}";

    public static implicit operator float(ValueFloat d) => d.Value;

    internal static ValueFloat Read(Parser parser) => new(parser.ReadFloat());

    internal static ValueArray ReadVector(Parser parser, int count) {
        throw new NotSupportedException();
        // return new(Enumerable.Range(0, count).Select(_ => (IValue?) Read(reader)).ToImmutableList());
    }
}
