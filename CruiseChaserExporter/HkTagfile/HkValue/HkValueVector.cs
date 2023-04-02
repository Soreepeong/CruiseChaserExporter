using System.Collections.Immutable;
using CruiseChaserExporter.HkTagfile.HkField;

namespace CruiseChaserExporter.HkTagfile.HkValue;

public class HkValueVector : IHkValue {
    public readonly IList<IHkValue?> Values;

    public HkValueVector(IList<IHkValue?> values) {
        Values = values;
    }

    public override string ToString() => Values.Count switch {
        0 => "HkValueVector(empty)",
        1 => "HkValueVector(1 item)",
        _ => $"HkValueVector({Values.Count} items)",
    };

    internal static HkValueVector Read(TagfileParser tagfileParser, HkFieldType? innerType) {
        if (innerType is null)
            throw new InvalidDataException("Array cannot have null innerType");
        return IHkValue.ReadVector(tagfileParser, innerType, tagfileParser.ReadInt());
    }

    internal static HkValueVector Read(TagfileParser tagfileParser, HkFieldType? innerType, int count) {
        if (innerType is null)
            throw new InvalidDataException("Array cannot have null innerType");
        return new(
            Enumerable.Range(0, count)
                .Select(_ => IHkValue.Read(tagfileParser, innerType))
                .ToImmutableList());
    }

    internal static HkValueVector ReadVector(TagfileParser tagfileParser, HkFieldType? innerType, int innerCount, int outerCount) {
        if (innerType is null)
            throw new InvalidDataException("Array cannot have null innerType");
        if (innerCount == 4)
            innerCount = tagfileParser.ReadInt();

        return new(Enumerable.Range(0, outerCount)
            .Select(_ => (IHkValue?) new HkValueVector(
                Enumerable.Range(0, innerCount)
                    .Select(_ => IHkValue.Read(tagfileParser, innerType))
                    .ToImmutableList()))
            .ToImmutableList());
    }

    public static readonly HkValueVector Empty = new(ImmutableList<IHkValue?>.Empty);
}