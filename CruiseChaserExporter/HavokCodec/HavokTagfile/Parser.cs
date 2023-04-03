using System.Text;

namespace CruiseChaserExporter.HavokCodec.HavokTagfile;

public class Parser {
    private const ulong HavokMagic = 0xD011FACECAB00D1E;

    internal readonly Dictionary<Tuple<string, int>, Definition> Definitions;
    internal readonly List<Definition?> OrderedDefinitions = new() {null};
    internal readonly List<Node?> Nodes = new();
    internal readonly List<int> References = new() {-1};
    internal readonly Dictionary<int, int> PendingReferences = new();

    private readonly BinaryReader _reader;
    private readonly List<string?> _strings = new() {"", null};

    private Parser(BinaryReader reader, Dictionary<Tuple<string, int>, Definition> definitions) {
        _reader = reader;
        Definitions = definitions;
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
            var tagType = (TagType) ReadInt();
            switch (tagType) {
                case TagType.Metadata:
                    Version = ReadInt();
                    if (Version != 3)
                        throw new NotSupportedException();
                    break;

                case TagType.Definition: {
                    var def = Definition.Read(this);
                    var defkey = Tuple.Create(def.Name, def.Version);
                    if (Definitions.TryGetValue(defkey, out var exdef))
                        def = exdef;
                    else
                        Definitions.Add(defkey, def);
                    OrderedDefinitions.Add(def);
                    break;
                }

                case TagType.Node:
                    Node.ReadAndInsert(this);
                    break;

                case TagType.EndOfFile:
                    var remainingFields = OrderedDefinitions
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
                            f.ReferenceDefinition = OrderedDefinitions.First(x => x?.Name == f.ReferencedName);
                    }

                    return;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public static Node Parse(
        BinaryReader reader,
        Dictionary<Tuple<string, int>, Definition>? definitions = null,
        bool closeAfter = false) {
        try {
            var parser = new Parser(reader, definitions ?? new());
            parser._Parse();
            return parser.Nodes.First()!;
        } finally {
            if (closeAfter)
                reader.Close();
        }
    }

    public static Node Parse(byte[] data, Dictionary<Tuple<string, int>, Definition>? definitions = null)
        => Parse(new BinaryReader(new MemoryStream(data)), definitions);
}
