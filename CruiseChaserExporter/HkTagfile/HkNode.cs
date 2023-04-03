using System.Collections.Immutable;
using CruiseChaserExporter.HkTagfile.HkValue;

namespace CruiseChaserExporter.HkTagfile;

public class HkNode {
    private Dictionary<string, IHkValue?>? _cachedAsMap;

    public readonly HkDefinition Definition;
    public readonly IList<IHkValue?> Values;

    public HkNode(HkDefinition definition, IList<IHkValue?> values) {
        Definition = definition;
        Values = values;
    }

    public Dictionary<string, IHkValue?> AsMap => _cachedAsMap ??= Definition
        .NestedFields
        .Zip(Values)
        .ToDictionary(x => x.First.Name, x => x.Second);

    public IHkValue? this[string key] => AsMap[key];

    public override string ToString() => Values.Count switch {
        0 => $"{Definition.Name} (empty)",
        1 => $"{Definition.Name} (1 value)",
        _ => $"{Definition.Name} ({Values.Count} value(s))",
    };

    internal static void ReadAndInsert(TagfileParser tagfileParser) {
        // Default to storing the node at the end of the node array.
        var nodeIndex = tagfileParser.Nodes.Count;

        // Check if it's already been requested. If it has,
        // we can use the pre-reserved node index rather than adding a new one.
        var refIndex = tagfileParser.References.Count;
        if (tagfileParser.PendingReferences.Remove(refIndex, out var targetNodeIndex))
            nodeIndex = targetNodeIndex;
        tagfileParser.References.Add(nodeIndex);

        // If the node is still intended to be placed at the end, reserve a position for it.
        if (nodeIndex == tagfileParser.Nodes.Count)
            tagfileParser.Nodes.Add(null);

        // Read & resolve the definition for this node.
        var definition = tagfileParser.Definitions[tagfileParser.ReadInt()];
        if (definition is null)
            throw new NullReferenceException();

        // Read fields. Order is guaranteed to follow definition fields, however
        // values may be sparse, as defined by the bitfield.
        var fieldMask = HkBitfield.Read(tagfileParser, definition.NestedFields.Count);
        var values = definition.NestedFields.Zip(fieldMask)
            .Select(x => x.Second ? IHkValue.Read(tagfileParser, x.First.FieldType) : null)
            .ToImmutableList();

        tagfileParser.Nodes[nodeIndex] = new(definition, values);
    }
}
