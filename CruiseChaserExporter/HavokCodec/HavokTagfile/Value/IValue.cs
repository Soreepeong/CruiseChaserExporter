using System.Collections.Immutable;
using CruiseChaserExporter.HavokCodec.HavokTagfile.Field;

namespace CruiseChaserExporter.HavokCodec.HavokTagfile.Value;

public interface IValue {
    public static IValue? Read(Parser parser, FieldType fieldType) =>
        fieldType.ArrayType switch {
            FieldArrayType.NotAnArray => fieldType.ElementType switch {
                FieldElementType.Void => null,
                FieldElementType.Byte => ValueByte.Read(parser),
                FieldElementType.Integer => ValueInt.Read(parser),
                FieldElementType.Float => ValueFloat.Read(parser),
                FieldElementType.Reference => ValueNode.ReadReference(parser),
                FieldElementType.Struct => ValueNode.ReadStruct(parser, fieldType.ReferencedName),
                FieldElementType.String => ValueString.Read(parser),
                FieldElementType.Array => throw new InvalidDataException("Inconsistent state detected"),
                _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "Single/ElementType")
            },
            FieldArrayType.VariableLength => ValueArray.Read(parser, fieldType.InnerType),
            FieldArrayType.FixedLength => ValueArray.Read(parser, fieldType.InnerType, fieldType.Length),
            _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "SequenceType")
        };

    public static ValueArray ReadVector(Parser parser, FieldType fieldType, int count) =>
        fieldType.ArrayType switch {
            FieldArrayType.NotAnArray => fieldType.ElementType switch {
                FieldElementType.Void => new(Enumerable.Range(0, count).Select(_ => (IValue?) null)
                    .ToImmutableList()),
                FieldElementType.Byte => ValueByte.ReadVector(parser, count),
                FieldElementType.Integer => ValueInt.ReadVector(parser, count),
                FieldElementType.Float => ValueFloat.ReadVector(parser, count),
                FieldElementType.Reference => ValueNode.ReadReferenceVector(parser, count),
                FieldElementType.Struct => ValueNode.ReadStructVector(parser, fieldType.ReferencedName,
                    count),
                FieldElementType.String => ValueString.ReadVector(parser, count),
                FieldElementType.Array => throw new InvalidDataException("Inconsistent state detected"),
                _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "Single/ElementType")
            },
            FieldArrayType.VariableLength => throw new NotSupportedException(),
            FieldArrayType.FixedLength => ValueArray.ReadVector(parser, fieldType.InnerType,
                fieldType.Length,
                count),
            _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "SequenceType")
        };
}
