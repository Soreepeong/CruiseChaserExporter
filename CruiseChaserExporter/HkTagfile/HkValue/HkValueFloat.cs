namespace CruiseChaserExporter.HkTagfile.HkValue;

public class HkValueFloat : IHkValue {
    public readonly float Value;

    public HkValueFloat(float value) {
        Value = value;
    }

    public override string ToString() => $"{Value}";

    public static implicit operator float(HkValueFloat d) => d.Value;

    internal static HkValueFloat Read(TagfileParser tagfileParser) => new(tagfileParser.ReadFloat());

    internal static HkValueVector ReadVector(TagfileParser tagfileParser, int count) {
        throw new NotSupportedException();
        // return new(Enumerable.Range(0, count).Select(_ => (IHkValue?) Read(reader)).ToImmutableList());
    }
}
