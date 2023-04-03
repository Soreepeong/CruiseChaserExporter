using System.Collections.Immutable;
using System.Text;
using CruiseChaserExporter.HavokCodec.HavokTagfile.Field;

namespace CruiseChaserExporter.HavokCodec.HavokTagfile;

public class Definition {
    public readonly string Name;
    public readonly int Version;
    public readonly Definition? Parent;
    public readonly ImmutableList<NamedField> Fields;
    public readonly ImmutableList<NamedField> NestedFields;

    public Definition(string name, int version, Definition? parent, ImmutableList<NamedField> fields) {
        Name = name;
        Version = version;
        Parent = parent;
        Fields = fields;
        NestedFields = (parent?.NestedFields ?? ImmutableList<NamedField>.Empty).Concat(Fields).ToImmutableList();
    }

    public override string ToString() => $"{Name}(v{Version})";

    public string GenerateCSharpCode() {
        var sb = new StringBuilder();
        var ns = GetType().Namespace!.Split(".");
        ns[^1] = "KnownDefinitions";
        sb.Append("namespace ").AppendJoin('.', ns).AppendLine(";");
        sb.AppendLine();
        sb.AppendLine("#pragma warning disable CS8618");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        var assemblyName = GetType().Assembly.GetName();
        sb.AppendLine($"[System.CodeDom.Compiler.GeneratedCode(\"{assemblyName.Name}\", \"{assemblyName.Version}\")]");
        sb.Append("public class ").Append(TypedHavokDeserializer.NormalizeName(Name));
        if (Parent != null)
            sb.Append(" : ").Append(TypedHavokDeserializer.NormalizeName(Parent.Name));
        sb.AppendLine(" {");

        foreach (var field in Fields) {
            var name = TypedHavokDeserializer.NormalizeName(field.Name);
            var fieldType = field.FieldType.GenerateCSharpTypeCode();
            switch (field.FieldType.ArrayType) {
                case FieldArrayType.NotAnArray:
                    sb.AppendLine(field.FieldType.ElementType switch {
                        FieldElementType.Byte => $"\tpublic byte {name};",
                        FieldElementType.Integer => $"\tpublic int {name};",
                        FieldElementType.Float => $"\tpublic float {name};",
                        _ => $"\tpublic {fieldType}? {name};"
                    });
                    break;
                case FieldArrayType.VariableLength:
                case FieldArrayType.FixedLength:
                    sb.AppendLine($"\tpublic {fieldType} {name};");
                    break;
                default:
                    throw new InvalidDataException();
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    internal static Definition Read(Parser parser) {
        var name = parser.ReadString();
        var version = parser.ReadInt();
        var parent = parser.OrderedDefinitions[parser.ReadInt()];
        var numFields = parser.ReadInt();
        var fields = Enumerable.Range(0, numFields).Select(_ => NamedField.Read(parser)).ToImmutableList();
        return new(name, version, parent, fields);
    }
}
