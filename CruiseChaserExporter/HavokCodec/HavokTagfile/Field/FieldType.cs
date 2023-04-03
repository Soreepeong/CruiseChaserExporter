namespace CruiseChaserExporter.HavokCodec.HavokTagfile.Field;

public class FieldType {
    public readonly FieldArrayType ArrayType;
    public readonly FieldElementType ElementType;
    public readonly FieldType? InnerType;
    public readonly string? ReferencedName;
    public readonly int Length;

    private FieldType(
        FieldElementType elementType,
        FieldArrayType arrayType = FieldArrayType.NotAnArray,
        FieldType? innerType = null,
        string? referencedName = null,
        int length = 0
    ) {
        ElementType = elementType;
        ArrayType = arrayType;
        InnerType = innerType;
        ReferencedName = referencedName;
        Length = length;
    }

    public Definition? ReferenceDefinition { get; internal set; }

    public override string ToString() => ReferencedName == null
        ? ArrayType switch {
            FieldArrayType.NotAnArray => $"{ElementType}",
            FieldArrayType.VariableLength when ElementType == FieldElementType.Array => $"{InnerType}[?]",
            FieldArrayType.FixedLength when ElementType == FieldElementType.Array => $"{InnerType}[{Length}]",
            _ => $"{ElementType}[INVALID]"
        }
        : ArrayType switch {
            FieldArrayType.NotAnArray => $"{ElementType}<{ReferencedName}>",
            FieldArrayType.VariableLength when ElementType == FieldElementType.Array =>
                $"{InnerType}<{ReferencedName}>[?]",
            FieldArrayType.FixedLength when ElementType == FieldElementType.Array =>
                $"{InnerType}<{ReferencedName}>[{Length}]",
            _ => $"{ElementType}<{ReferencedName}>[INVALID]"
        };

    public string GenerateCSharpTypeCode() {
        switch (ArrayType) {
            case FieldArrayType.NotAnArray:
                switch (ElementType) {
                    case FieldElementType.Void:
                        return "object";
                    case FieldElementType.Byte:
                        return "byte";
                    case FieldElementType.Integer:
                        return "int";
                    case FieldElementType.Float:
                        return "float";
                    case FieldElementType.Reference:
                        return ReferencedName != null
                            ? TypedHavokDeserializer.NormalizeName(ReferencedName!)
                            : throw new NullReferenceException();
                    case FieldElementType.Struct:
                        return ReferencedName != null
                            ? TypedHavokDeserializer.NormalizeName(ReferencedName!)
                            : throw new NullReferenceException();
                    case FieldElementType.String:
                        return "string";
                    case FieldElementType.Array:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            case FieldArrayType.VariableLength:
            case FieldArrayType.FixedLength:
                return $"{InnerType!.GenerateCSharpTypeCode()}[]";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    internal static FieldType Read(Parser parser) {
        var rawType = parser.ReadInt();
        var storedType = (FieldStoredType) (rawType & 0xF);
        var sequenceType = (FieldArrayType) (rawType >> 4);

        var fixedLength = sequenceType == FieldArrayType.FixedLength ? parser.ReadInt() : 0;

        var fieldType = storedType switch {
            FieldStoredType.Void => SingleVoid,
            FieldStoredType.Byte => SingleByte,
            FieldStoredType.Integer => SingleInteger,
            FieldStoredType.Float => SingleFloat,
            FieldStoredType.Array4 => FixedFloatArray(4),
            FieldStoredType.Array8 => FixedFloatArray(8),
            FieldStoredType.Array12 => FixedFloatArray(12),
            FieldStoredType.Array16 => FixedFloatArray(16),
            FieldStoredType.Reference => Reference(parser.ReadString()),
            FieldStoredType.Struct => Struct(parser.ReadString()),
            FieldStoredType.String => SingleString,
            _ => throw new ArgumentOutOfRangeException(nameof(storedType), storedType, null)
        };

        return sequenceType switch {
            FieldArrayType.FixedLength => WrapFixedArray(fieldType, fixedLength),
            FieldArrayType.VariableLength => WrapVariableArray(fieldType),
            _ => fieldType
        };
    }

    public static FieldType FixedFloatArray(int length) => new(
        elementType: FieldElementType.Array,
        arrayType: FieldArrayType.FixedLength,
        innerType: SingleFloat,
        length: length);

    public static FieldType Reference(string referenceName) => new(
        elementType: FieldElementType.Reference,
        referencedName: referenceName);

    public static FieldType Struct(string structName) => new(
        elementType: FieldElementType.Struct,
        referencedName: structName);

    public static FieldType WrapFixedArray(FieldType innerType, int length) => new(
        elementType: FieldElementType.Array,
        arrayType: FieldArrayType.FixedLength,
        innerType: innerType,
        length: length);

    public static FieldType WrapVariableArray(FieldType innerType) => new(
        elementType: FieldElementType.Array,
        arrayType: FieldArrayType.VariableLength,
        innerType: innerType);

    public static readonly FieldType SingleVoid = new(FieldElementType.Void);
    public static readonly FieldType SingleByte = new(FieldElementType.Byte);
    public static readonly FieldType SingleInteger = new(FieldElementType.Integer);
    public static readonly FieldType SingleFloat = new(FieldElementType.Float);
    public static readonly FieldType SingleString = new(FieldElementType.String);
}
