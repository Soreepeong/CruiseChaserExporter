using System.Collections.Immutable;
using System.Text;
using CruiseChaserExporter.HkTagfile.HkField;

namespace CruiseChaserExporter.HkTagfile;

public class HkDefinition {
    public readonly string Name;
    public readonly int Version;
    public readonly HkDefinition? Parent;
    public readonly ImmutableList<HkNamedField> Fields;
    public readonly ImmutableList<HkNamedField> NestedFields;

    public HkDefinition(string name, int version, HkDefinition? parent, ImmutableList<HkNamedField> fields) {
        Name = name;
        Version = version;
        Parent = parent;
        Fields = fields;
        NestedFields = (parent?.NestedFields ?? ImmutableList<HkNamedField>.Empty).Concat(Fields).ToImmutableList();
    }

    public override string ToString() => $"{Name}(v{Version})";

    public string GenerateCSharpCode(Func<string, string>? nameTransformer) {
        nameTransformer ??= x => x;
        
        var sb = new StringBuilder();
        sb.Append("public class ").Append(nameTransformer(Name));
        if (Parent != null)
            sb.Append(" : ").Append(nameTransformer(Parent.Name));
        sb.Append(" {\n");

        foreach (var field in Fields) {
            var name = nameTransformer(field.Name);
            var fieldType = field.FieldType.GenerateCSharpTypeCode(nameTransformer);
            switch (field.FieldType.ArrayType) {
                case HkFieldArrayType.NotAnArray:
                    sb.Append($"\tpublic {fieldType}? {name};\n");
                    break;
                case HkFieldArrayType.VariableLength:
                case HkFieldArrayType.FixedLength:
                    sb.Append($"\tpublic {fieldType} {name};\n");
                    break;
                default:
                    throw new InvalidDataException();
            }
        }

        sb.Append("}\n");
        return sb.ToString();
    }

    internal static HkDefinition Read(TagfileParser tagfileParser) { 
        var name = tagfileParser.ReadString();
        var version = tagfileParser.ReadInt();
        var parent = tagfileParser.Definitions[tagfileParser.ReadInt()];
        var numFields = tagfileParser.ReadInt();
        var fields = Enumerable.Range(0, numFields).Select(_ => HkNamedField.Read(tagfileParser)).ToImmutableList();
        return new(name, version, parent, fields);
    }
}