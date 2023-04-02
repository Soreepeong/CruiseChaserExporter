using System.Collections.Immutable;
using CruiseChaserExporter.HkTagfile.HkField;

namespace CruiseChaserExporter.HkTagfile.HkValue;

public interface IHkValue {
    public static IHkValue? Read(TagfileParser tagfileParser, HkFieldType fieldType) =>
        fieldType.ArrayType switch {
            HkFieldArrayType.NotAnArray => fieldType.ElementType switch {
                HkFieldElementType.Void => null,
                HkFieldElementType.Byte => HkValueByte.Read(tagfileParser),
                HkFieldElementType.Integer => HkValueInt.Read(tagfileParser),
                HkFieldElementType.Float => HkValueFloat.Read(tagfileParser),
                HkFieldElementType.Reference => HkValueNode.ReadReference(tagfileParser),
                HkFieldElementType.Struct => HkValueNode.ReadStruct(tagfileParser, fieldType.ReferencedName),
                HkFieldElementType.String => HkValueString.Read(tagfileParser),
                HkFieldElementType.Array => throw new InvalidDataException("Inconsistent state detected"),
                _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "Single/ElementType")
            },
            HkFieldArrayType.VariableLength => HkValueVector.Read(tagfileParser, fieldType.InnerType),
            HkFieldArrayType.FixedLength => HkValueVector.Read(tagfileParser, fieldType.InnerType, fieldType.Length),
            _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "SequenceType")
        };

    public static HkValueVector ReadVector(TagfileParser tagfileParser, HkFieldType fieldType, int count) =>
        fieldType.ArrayType switch {
            HkFieldArrayType.NotAnArray => fieldType.ElementType switch {
                HkFieldElementType.Void => new(Enumerable.Range(0, count).Select(_ => (IHkValue?) null)
                    .ToImmutableList()),
                HkFieldElementType.Byte => HkValueByte.ReadVector(tagfileParser, count),
                HkFieldElementType.Integer => HkValueInt.ReadVector(tagfileParser, count),
                HkFieldElementType.Float => HkValueFloat.ReadVector(tagfileParser, count),
                HkFieldElementType.Reference => HkValueNode.ReadReferenceVector(tagfileParser, count),
                HkFieldElementType.Struct => HkValueNode.ReadStructVector(tagfileParser, fieldType.ReferencedName, count),
                HkFieldElementType.String => HkValueString.ReadVector(tagfileParser, count),
                HkFieldElementType.Array => throw new InvalidDataException("Inconsistent state detected"),
                _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "Single/ElementType")
            },
            HkFieldArrayType.VariableLength => throw new NotSupportedException(),
            HkFieldArrayType.FixedLength => HkValueVector.ReadVector(tagfileParser, fieldType.InnerType, fieldType.Length,
                count),
            _ => throw new ArgumentOutOfRangeException(nameof(fieldType), fieldType, "SequenceType")
        };
}