using System.Collections.Immutable;
using System.Text;
using CruiseChaserExporter.HkTagfile.HkField;

namespace CruiseChaserExporter.HkTagfile;

public class TagfileParser {
    private const ulong HavokMagic = 0xD011FACECAB00D1E;

    internal readonly List<HkDefinition?> Definitions = new() {null};
    internal readonly List<HkNode?> Nodes = new();
    internal readonly List<int> References = new() {-1};
    internal readonly Dictionary<int, int> PendingReferences = new();

    private readonly BinaryReader _reader;
    private readonly List<string?> _strings = new() {"", null};

    private TagfileParser(BinaryReader reader) {
        _reader = reader;
    }

    public int Version { get; private set; }

    internal int ReadInt() {
        var b = _reader.ReadByte();
        var sign = (b & 1) == 1 ? -1 : 1;
        var val = (b >> 1) & 0x3F;

        var shift = 6;
        while ((b & 0x80) != 0) {
            b = _reader.ReadByte();
            val |= (b & 0x7F) << shift;
            shift += 7;
        }

        return val * sign;
    }

    internal float ReadFloat() => _reader.ReadSingle();

    internal byte ReadByte() => _reader.ReadByte();

    internal byte[] ReadBytes(int length) => _reader.ReadBytes(length);

    internal string? ReadStringNullable() {
        var indexOrLength = ReadInt();
        if (indexOrLength <= 0)
            return _strings[-indexOrLength];

        _strings.Add(Encoding.UTF8.GetString(_reader.ReadBytes(indexOrLength)));
        return _strings[^1];
    }

    internal string ReadString() {
        var res = ReadStringNullable();
        if (res is null)
            throw new NullReferenceException();
        return res;
    }

    private void _Parse() {
        if (_reader.ReadUInt64() != HavokMagic)
            throw new InvalidDataException();

        while (true) {
            var tagType = (HkTagType) ReadInt();
            switch (tagType) {
                case HkTagType.Metadata:
                    Version = ReadInt();
                    if (Version != 3)
                        throw new NotSupportedException();
                    break;

                case HkTagType.Definition:
                    Definitions.Add(HkDefinition.Read(this));
                    break;

                case HkTagType.Node:
                    HkNode.ReadAndInsert(this);
                    break;

                case HkTagType.EndOfFile:
                    var remainingFields = Definitions
                        .Where(x => x is not null)
                        .SelectMany(x => x!.Fields)
                        .Select(x => x.FieldType)
                        .ToList();
                    var knownFields = remainingFields.ToHashSet();
                    while (remainingFields.Any()) {
                        var f = remainingFields[^1];
                        remainingFields.RemoveAt(remainingFields.Count - 1);

                        if (f.InnerType is not null && !knownFields.Contains(f.InnerType)) {
                            knownFields.Add(f.InnerType);
                            remainingFields.Add(f.InnerType);
                        }

                        if (f.ReferencedName is not null)
                            f.ReferenceDefinition = Definitions.First(x => x?.Name == f.ReferencedName);
                    }

                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public static void Parse(
        out HkNode rootNode,
        out ImmutableList<HkDefinition> definitions,
        BinaryReader reader,
        bool closeAfter = false) {
        try {
            var parser = new TagfileParser(reader);
            parser._Parse();
            rootNode = parser.Nodes.First()!;
            definitions = parser.Definitions.Where(x => x != null).Select(x => x!).ToImmutableList();
        } finally {
            if (closeAfter)
                reader.Close();
        }
    }
}
