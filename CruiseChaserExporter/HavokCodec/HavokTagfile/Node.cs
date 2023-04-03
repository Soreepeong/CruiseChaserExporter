using System.Collections.Immutable;
using CruiseChaserExporter.HavokCodec.HavokTagfile.Value;

namespace CruiseChaserExporter.HavokCodec.HavokTagfile;

public class Node {
    private Dictionary<string, IValue?>? _cachedAsMap;

    public readonly Definition Definition;
    public readonly IList<IValue?> Values;

    public Node(Definition definition, IList<IValue?> values) {
        Definition = definition;
        Values = values;
    }

    public Dictionary<string, IValue?> AsMap => _cachedAsMap ??= Definition
        .NestedFields
        .Zip(Values)
        .ToDictionary(x => x.First.Name, x => x.Second);

    public IValue? this[string key] => AsMap[key];

    public override string ToString() => Values.Count switch {
        0 => $"{Definition.Name} (empty)",
        1 => $"{Definition.Name} (1 value)",
        _ => $"{Definition.Name} ({Values.Count} value(s))",
    };

    internal static void ReadAndInsert(Parser parser) {
        // Default to storing the node at the end of the node array.
        var nodeIndex = parser.Nodes.Count;

        // Check if it's already been requested. If it has,
        // we can use the pre-reserved node index rather than adding a new one.
        var refIndex = parser.References.Count;
        if (parser.PendingReferences.Remove(refIndex, out var targetNodeIndex))
            nodeIndex = targetNodeIndex;
        parser.References.Add(nodeIndex);

        // If the node is still intended to be placed at the end, reserve a position for it.
        if (nodeIndex == parser.Nodes.Count)
            parser.Nodes.Add(null);

        // Read & resolve the definition for this node.
        var definition = parser.OrderedDefinitions[parser.ReadInt()];
        if (definition is null)
            throw new NullReferenceException();

        // Read fields. Order is guaranteed to follow definition fields, however
        // values may be sparse, as defined by the bitfield.
        var fieldMask = Bitfield.Read(parser, definition.NestedFields.Count);
        var values = definition.NestedFields.Zip(fieldMask)
            .Select(x => x.Second ? IValue.Read(parser, x.First.FieldType) : null)
            .ToImmutableList();

        parser.Nodes[nodeIndex] = new(definition, values);
    }
}
