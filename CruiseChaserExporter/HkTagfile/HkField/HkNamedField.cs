namespace CruiseChaserExporter.HkTagfile.HkField;

public class HkNamedField {
    public readonly string Name;
    public readonly HkFieldType FieldType;

    public HkNamedField(string name, HkFieldType fieldType) {
        Name = name;
        FieldType = fieldType;
    }

    public override string ToString() => $"{Name}({FieldType})";

    internal static HkNamedField Read(TagfileParser tagfileParser) {
        var name = tagfileParser.ReadString();
        var fieldType = HkFieldType.Read(tagfileParser);
        return new(name, fieldType);
    }
}
