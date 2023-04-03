using CruiseChaserExporter.HavokCodec.HavokTagfile.Field;
using CruiseChaserExporter.HavokCodec.HavokTagfile.Value;
using CruiseChaserExporter.Util;

namespace CruiseChaserExporter.HavokCodec.HavokTagfile;

public class Deserializer {
    private static readonly TaggedLogger Log = new(typeof(Deserializer).AssemblyQualifiedName!);
    
    private readonly Dictionary<Tuple<string, int>, Type> _typeMap;
    private readonly Dictionary<Node, object> _referenceMap;
    private readonly Func<string, string> _nameTransformer;

    private Deserializer(Dictionary<Definition, Type> definitions,
        Func<string, string>? nameTransformer = null) {
        _nameTransformer = nameTransformer ?? (x => x);
        _typeMap = definitions.ToDictionary(x => Tuple.Create(_nameTransformer(x.Key.Name), x.Key.Version),
            x => x.Value);
        _referenceMap = new();
    }

    private object _unserializeNode(Node node) {
        if (_referenceMap.TryGetValue(node, out var obj))
            return obj;

        var t = _toClrType(node.Definition);
        _referenceMap[node] = obj = t.GetConstructor(Array.Empty<Type>())!.Invoke(null);
        foreach (var (hkField, value) in node.Definition.NestedFields.Zip(node.Values)) {
            var fieldName = _nameTransformer(hkField.Name);
            var field = t.GetField(fieldName);
            if (field is null) {
                Log.W($"Unsupported field: {node.Definition.Name}(v{node.Definition.Version}).{fieldName}");
                continue;
            }
                
            var unwrapped = Unwrap(hkField.FieldType, value);
            field.SetValue(obj, unwrapped);
        }

        return obj;
    }

    private Type _toClrType(Definition definition) =>
        _typeMap[Tuple.Create(_nameTransformer(definition.Name), definition.Version)];

    private Type _toClrType(FieldType fieldType) {
        switch (fieldType.ArrayType) {
            case FieldArrayType.NotAnArray:
                return fieldType.ElementType switch {
                    FieldElementType.Void => typeof(object),
                    FieldElementType.Byte => typeof(byte),
                    FieldElementType.Integer => typeof(int),
                    FieldElementType.Float => typeof(float),
                    FieldElementType.Reference or FieldElementType.Struct =>
                        _toClrType(fieldType.ReferenceDefinition!),
                    FieldElementType.String => typeof(string),
                    _ => throw new InvalidDataException()
                };
            case FieldArrayType.VariableLength:
            case FieldArrayType.FixedLength:
                return fieldType.InnerType!.ElementType switch {
                    FieldElementType.Void => typeof(object?[]),
                    FieldElementType.Byte => typeof(byte[]),
                    FieldElementType.Integer => typeof(int[]),
                    FieldElementType.Float => typeof(float[]),
                    FieldElementType.Array or FieldElementType.Reference or FieldElementType.Struct =>
                        _toClrType(fieldType.InnerType!).MakeArrayType(),
                    FieldElementType.String => typeof(string[]),
                    _ => throw new InvalidDataException()
                };
            default:
                throw new InvalidDataException();
        }
    }

    public object? Unwrap(
        FieldType fieldType,
        IValue? value) {
        switch (fieldType.ArrayType) {
            case FieldArrayType.NotAnArray:
                return value switch {
                    null => null,
                    ValueByte vbyte => vbyte.Value,
                    ValueInt vbyte => vbyte.Value,
                    ValueFloat vbyte => vbyte.Value,
                    ValueNode vnode => _unserializeNode(vnode.Node),
                    ValueString vbyte => vbyte.Value,
                    _ => throw new InvalidDataException()
                };
            case FieldArrayType.VariableLength:
            case FieldArrayType.FixedLength: {
                if (value is null)
                    return Array.CreateInstance(_toClrType(fieldType.InnerType!), fieldType.Length);

                if (value is not ValueArray vlist)
                    throw new InvalidCastException();

                switch (fieldType.InnerType!.ElementType) {
                    case FieldElementType.Void:
                        return vlist.Values.Select(_ => (object?) null).ToArray();
                    case FieldElementType.Byte:
                        return vlist.Values.Select(x => ((ValueByte) x!).Value).ToArray();
                    case FieldElementType.Integer:
                        return vlist.Values.Select(x => ((ValueInt) x!).Value).ToArray();
                    case FieldElementType.Float:
                        return vlist.Values.Select(x => ((ValueFloat) x!).Value).ToArray();
                    case FieldElementType.Array:
                    case FieldElementType.Reference:
                    case FieldElementType.Struct: {
                        var obj = Array.CreateInstance(_toClrType(fieldType.InnerType), vlist.Values.Count);
                        for (var i = 0; i < vlist.Values.Count; i++)
                            obj.SetValue(Unwrap(fieldType.InnerType!, vlist.Values[i]), i);
                        return obj;
                    }
                    case FieldElementType.String:
                        return vlist.Values.Select(x => ((ValueString) x!).Value).ToArray();
                    default:
                        throw new InvalidDataException();
                }
            }
            default:
                throw new InvalidDataException();
        }
    }

    public static T Deserialize<T>(
        Node node,
        Dictionary<Definition, Type> definitions,
        Func<string, string>? nameTransformer = null) =>
        (T) new Deserializer(definitions, nameTransformer)._unserializeNode(node);
}
