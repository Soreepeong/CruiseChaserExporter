using System.Collections.Immutable;
using CruiseChaserExporter.HavokCodec.HavokTagfile.Field;

namespace CruiseChaserExporter.HavokCodec.HavokTagfile.Value;

public class ValueArray : IValue {
    public readonly IList<IValue?> Values;

    public ValueArray(IList<IValue?> values) {
        Values = values;
    }

    public override string ToString() => Values.Count switch {
        0 => "ValueArray(empty)",
        1 => "ValueArray(1 item)",
        _ => $"ValueArray({Values.Count} items)",
    };

    internal static ValueArray Read(Parser parser, FieldType? innerType) {
        if (innerType is null)
            throw new InvalidDataException("Array cannot have null innerType");
        return IValue.ReadVector(parser, innerType, parser.ReadInt());
    }

    internal static ValueArray Read(Parser parser, FieldType? innerType, int count) {
        if (innerType is null)
            throw new InvalidDataException("Array cannot have null innerType");
        return new(
            Enumerable.Range(0, count)
                .Select(_ => IValue.Read(parser, innerType))
                .ToImmutableList());
    }

    internal static ValueArray ReadVector(Parser parser, FieldType? innerType, int innerCount,
        int outerCount) {
        if (innerType is null)
            throw new InvalidDataException("Array cannot have null innerType");
        if (innerCount == 4)
            innerCount = parser.ReadInt();

        return new(Enumerable.Range(0, outerCount)
            .Select(_ => (IValue?) new ValueArray(
                Enumerable.Range(0, innerCount)
                    .Select(_ => IValue.Read(parser, innerType))
                    .ToImmutableList()))
            .ToImmutableList());
    }

    public static readonly ValueArray Empty = new(ImmutableList<IValue?>.Empty);
}
