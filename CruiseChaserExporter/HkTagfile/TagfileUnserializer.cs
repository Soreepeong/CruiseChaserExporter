﻿using CruiseChaserExporter.HkTagfile.HkField;
using CruiseChaserExporter.HkTagfile.HkValue;

namespace CruiseChaserExporter.HkTagfile;

public class TagfileUnserializer {
    private readonly Dictionary<Tuple<string, int>, Type> _typeMap;
    private readonly Dictionary<HkNode, object> _referenceMap;
    private readonly Func<string, string> _nameTransformer;

    private TagfileUnserializer(Dictionary<HkDefinition, Type> definitions,
        Func<string, string>? nameTransformer = null) {
        _nameTransformer = nameTransformer ?? (x => x);
        _typeMap = definitions.ToDictionary(x => Tuple.Create(_nameTransformer(x.Key.Name), x.Key.Version),
            x => x.Value);
        _referenceMap = new();
    }

    private object _unserializeNode(HkNode node) {
        if (_referenceMap.TryGetValue(node, out var obj))
            return obj;

        var t = _toClrType(node.Definition);
        _referenceMap[node] = obj = t.GetConstructor(Array.Empty<Type>())!.Invoke(null);
        foreach (var (hkField, value) in node.Definition.NestedFields.Zip(node.Values)) {
            var fieldName = _nameTransformer(hkField.Name);
            var field = t.GetField(fieldName)!;
            var unwrapped = Unwrap(hkField.FieldType, value);
            field.SetValue(obj, unwrapped);
        }

        return obj;
    }

    private Type _toClrType(HkDefinition definition) =>
        _typeMap[Tuple.Create(_nameTransformer(definition.Name), definition.Version)];

    private Type _toClrType(HkFieldType fieldType) {
        switch (fieldType.ArrayType) {
            case HkFieldArrayType.NotAnArray:
                return fieldType.ElementType switch {
                    HkFieldElementType.Void => typeof(object),
                    HkFieldElementType.Byte => typeof(byte),
                    HkFieldElementType.Integer => typeof(int),
                    HkFieldElementType.Float => typeof(float),
                    HkFieldElementType.Reference or HkFieldElementType.Struct =>
                        _toClrType(fieldType.ReferenceDefinition!),
                    HkFieldElementType.String => typeof(string),
                    _ => throw new InvalidDataException()
                };
            case HkFieldArrayType.VariableLength:
                return fieldType.InnerType!.ElementType switch {
                    HkFieldElementType.Void => typeof(List<object?>),
                    HkFieldElementType.Byte => typeof(List<byte>),
                    HkFieldElementType.Integer => typeof(List<int>),
                    HkFieldElementType.Float => typeof(List<float>),
                    HkFieldElementType.Array or HkFieldElementType.Reference or HkFieldElementType.Struct =>
                        typeof(List<>).MakeGenericType(_toClrType(fieldType.InnerType!)),
                    HkFieldElementType.String => typeof(List<string>),
                    _ => throw new InvalidDataException()
                };
            case HkFieldArrayType.FixedLength:
                return fieldType.InnerType!.ElementType switch {
                    HkFieldElementType.Void => typeof(object?[]),
                    HkFieldElementType.Byte => typeof(byte[]),
                    HkFieldElementType.Integer => typeof(int[]),
                    HkFieldElementType.Float => typeof(float[]),
                    HkFieldElementType.Array or HkFieldElementType.Reference or HkFieldElementType.Struct =>
                        _toClrType(fieldType.InnerType!).MakeArrayType(),
                    HkFieldElementType.String => typeof(string[]),
                    _ => throw new InvalidDataException()
                };
            default:
                throw new InvalidDataException();
        }
    }

    public object? Unwrap(
        HkFieldType fieldType,
        IHkValue? value) {
        switch (fieldType.ArrayType) {
            case HkFieldArrayType.NotAnArray:
                return value switch {
                    null => null,
                    HkValueByte vbyte => vbyte.Value,
                    HkValueInt vbyte => vbyte.Value,
                    HkValueFloat vbyte => vbyte.Value,
                    HkValueNode vnode => _unserializeNode(vnode.Node),
                    HkValueString vbyte => vbyte.Value,
                    _ => throw new InvalidDataException()
                };
            case HkFieldArrayType.VariableLength: {
                if (value is null)
                    return _toClrType(fieldType).GetConstructor(Type.EmptyTypes)!.Invoke(Array.Empty<object?>());
                
                if (value is not HkValueVector vlist)
                    throw new InvalidCastException();

                switch (fieldType.InnerType!.ElementType) {
                    case HkFieldElementType.Void:
                        return vlist.Values.Select(_ => (object?) null).ToList();
                    case HkFieldElementType.Byte:
                        return vlist.Values.Select(x => ((HkValueByte) x!).Value).ToList();
                    case HkFieldElementType.Integer:
                        return vlist.Values.Select(x => ((HkValueInt) x!).Value).ToList();
                    case HkFieldElementType.Float:
                        return vlist.Values.Select(x => ((HkValueFloat) x!).Value).ToList();
                    case HkFieldElementType.Array:
                    case HkFieldElementType.Reference:
                    case HkFieldElementType.Struct: {
                        var t = _toClrType(fieldType);
                        var obj = t.GetConstructor(Type.EmptyTypes)!.Invoke(Array.Empty<object?>());
                        var add = t.GetMethod("Add")!;
                        foreach (var v in vlist.Values)
                            add.Invoke(obj, new[] {Unwrap(fieldType.InnerType!, v)});
                        return obj;
                    }
                    case HkFieldElementType.String:
                        return vlist.Values.Select(x => ((HkValueString) x!).Value).ToList();
                    default:
                        throw new InvalidDataException();
                }
            }
            case HkFieldArrayType.FixedLength: {
                if (value is null)
                    return Array.CreateInstance(_toClrType(fieldType.InnerType!), fieldType.Length);

                if (value is not HkValueVector vlist)
                    throw new InvalidCastException();

                switch (fieldType.InnerType!.ElementType) {
                    case HkFieldElementType.Void:
                        return vlist.Values.Select(_ => (object?) null).ToArray();
                    case HkFieldElementType.Byte:
                        return vlist.Values.Select(x => ((HkValueByte) x!).Value).ToArray();
                    case HkFieldElementType.Integer:
                        return vlist.Values.Select(x => ((HkValueInt) x!).Value).ToArray();
                    case HkFieldElementType.Float:
                        return vlist.Values.Select(x => ((HkValueFloat) x!).Value).ToArray();
                    case HkFieldElementType.Array:
                    case HkFieldElementType.Reference:
                    case HkFieldElementType.Struct: {
                        var obj = Array.CreateInstance(_toClrType(fieldType.InnerType), fieldType.Length);
                        for (var i = 0; i < fieldType.Length; i++)
                            obj.SetValue(Unwrap(fieldType.InnerType!, vlist.Values[i]), i);
                        return obj;
                    }
                    case HkFieldElementType.String:
                        return vlist.Values.Select(x => ((HkValueString) x!).Value).ToArray();
                    default:
                        throw new InvalidDataException();
                }
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static T Unserialize<T>(
        HkNode node,
        Dictionary<HkDefinition, Type> definitions,
        Func<string, string>? nameTransformer = null) =>
        (T) new TagfileUnserializer(definitions, nameTransformer)._unserializeNode(node);
}