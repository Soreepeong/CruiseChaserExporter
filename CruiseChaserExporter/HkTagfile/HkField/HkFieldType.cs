namespace CruiseChaserExporter.HkTagfile.HkField;

public class HkFieldType {
    public readonly HkFieldArrayType ArrayType;
    public readonly HkFieldElementType ElementType;
    public readonly HkFieldType? InnerType;
    public readonly string? ReferencedName;
    public readonly int Length;

    private HkFieldType(
        HkFieldElementType elementType,
        HkFieldArrayType arrayType = HkFieldArrayType.NotAnArray,
        HkFieldType? innerType = null,
        string? referencedName = null,
        int length = 0
    ) {
        ElementType = elementType;
        ArrayType = arrayType;
        InnerType = innerType;
        ReferencedName = referencedName;
        Length = length;
    }

    public HkDefinition? ReferenceDefinition { get; internal set; }

    public override string ToString() => ReferencedName == null
        ? ArrayType switch {
            HkFieldArrayType.NotAnArray => $"{ElementType}",
            HkFieldArrayType.VariableLength when ElementType == HkFieldElementType.Array => $"{InnerType}[?]",
            HkFieldArrayType.FixedLength when ElementType == HkFieldElementType.Array => $"{InnerType}[{Length}]",
            _ => $"{ElementType}[INVALID]"
        }
        : ArrayType switch {
            HkFieldArrayType.NotAnArray => $"{ElementType}<{ReferencedName}>",
            HkFieldArrayType.VariableLength when ElementType == HkFieldElementType.Array =>
                $"{InnerType}<{ReferencedName}>[?]",
            HkFieldArrayType.FixedLength when ElementType == HkFieldElementType.Array =>
                $"{InnerType}<{ReferencedName}>[{Length}]",
            _ => $"{ElementType}<{ReferencedName}>[INVALID]"
        };

    public string GenerateCSharpTypeCode(Func<string, string>? nameTransformer) {
        nameTransformer ??= x => x;
        switch (ArrayType) {
            case HkFieldArrayType.NotAnArray:
                switch (ElementType) {
                    case HkFieldElementType.Void:
                        return "object";
                    case HkFieldElementType.Byte:
                        return "byte";
                    case HkFieldElementType.Integer:
                        return "int";
                    case HkFieldElementType.Float:
                        return "float";
                    case HkFieldElementType.Reference:
                        return ReferencedName != null
                            ? nameTransformer(ReferencedName!)
                            : throw new NullReferenceException();
                    case HkFieldElementType.Struct:
                        return ReferencedName != null
                            ? nameTransformer(ReferencedName!)
                            : throw new NullReferenceException();
                    case HkFieldElementType.String:
                        return "string";
                    case HkFieldElementType.Array:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            case HkFieldArrayType.VariableLength:
            case HkFieldArrayType.FixedLength:
                return $"{InnerType!.GenerateCSharpTypeCode(nameTransformer)}[]";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    internal static HkFieldType Read(TagfileParser tagfileParser) {
        var rawType = tagfileParser.ReadInt();
        var storedType = (HkFieldStoredEnum) (rawType & 0xF);
        var sequenceType = (HkFieldArrayType) (rawType >> 4);

        var fixedLength = sequenceType == HkFieldArrayType.FixedLength ? tagfileParser.ReadInt() : 0;

        var fieldType = storedType switch {
            HkFieldStoredEnum.Void => SingleVoid,
            HkFieldStoredEnum.Byte => SingleByte,
            HkFieldStoredEnum.Integer => SingleInteger,
            HkFieldStoredEnum.Float => SingleFloat,
            HkFieldStoredEnum.Array4 => FixedFloatArray(4),
            HkFieldStoredEnum.Array8 => FixedFloatArray(8),
            HkFieldStoredEnum.Array12 => FixedFloatArray(12),
            HkFieldStoredEnum.Array16 => FixedFloatArray(16),
            HkFieldStoredEnum.Reference => Reference(tagfileParser.ReadString()),
            HkFieldStoredEnum.Struct => Struct(tagfileParser.ReadString()),
            HkFieldStoredEnum.String => SingleString,
            _ => throw new ArgumentOutOfRangeException(nameof(storedType), storedType, null)
        };

        return sequenceType switch {
            HkFieldArrayType.FixedLength => WrapFixedArray(fieldType, fixedLength),
            HkFieldArrayType.VariableLength => WrapVariableArray(fieldType),
            _ => fieldType
        };
    }

    public static HkFieldType FixedFloatArray(int length) => new(
        elementType: HkFieldElementType.Array,
        arrayType: HkFieldArrayType.FixedLength,
        innerType: SingleFloat,
        length: length);

    public static HkFieldType Reference(string referenceName) => new(
        elementType: HkFieldElementType.Reference,
        referencedName: referenceName);

    public static HkFieldType Struct(string structName) => new(
        elementType: HkFieldElementType.Struct,
        referencedName: structName);

    public static HkFieldType WrapFixedArray(HkFieldType innerType, int length) => new(
        elementType: HkFieldElementType.Array,
        arrayType: HkFieldArrayType.FixedLength,
        innerType: innerType,
        length: length);

    public static HkFieldType WrapVariableArray(HkFieldType innerType) => new(
        elementType: HkFieldElementType.Array,
        arrayType: HkFieldArrayType.VariableLength,
        innerType: innerType);

    public static readonly HkFieldType SingleVoid = new(HkFieldElementType.Void);
    public static readonly HkFieldType SingleByte = new(HkFieldElementType.Byte);
    public static readonly HkFieldType SingleInteger = new(HkFieldElementType.Integer);
    public static readonly HkFieldType SingleFloat = new(HkFieldElementType.Float);
    public static readonly HkFieldType SingleString = new(HkFieldElementType.String);
}
