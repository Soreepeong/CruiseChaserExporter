using System.Collections.Immutable;

namespace CruiseChaserExporter.HkTagfile.HkValue;

public class HkValueNode : IHkValue {
    private readonly IList<HkNode?> _nodes;
    private readonly int _nodeIndex;

    public HkValueNode(IList<HkNode?> nodes, int nodeIndex) {
        _nodes = nodes;
        _nodeIndex = nodeIndex;
    }

    public HkNode Node => 0 <= _nodeIndex && _nodeIndex < _nodes.Count && _nodes[_nodeIndex] != null
        ? _nodes[_nodeIndex]!
        : throw new NullReferenceException(DescribeNode());

    public override string ToString() => $"Node#{_nodeIndex} ({DescribeNode()})";

    private string DescribeNode() => _nodeIndex < 0 || _nodes.Count <= _nodeIndex
        ? "<out of range>"
        : _nodes[_nodeIndex]?.ToString() ?? "<not yet available>";

    public static implicit operator HkNode(HkValueNode d) => d.Node;

    internal static HkValueNode ReadReference(TagfileParser tagfileParser) {
        var refIndex = tagfileParser.ReadInt();
        if (refIndex < tagfileParser.References.Count)
            return new(tagfileParser.Nodes, tagfileParser.References[refIndex]);

        if (!tagfileParser.PendingReferences.TryGetValue(refIndex, out var reservedNodeIndex)) {
            reservedNodeIndex = tagfileParser.PendingReferences[refIndex] = tagfileParser.Nodes.Count;
            tagfileParser.Nodes.Add(null);
        }

        return new(tagfileParser.Nodes, reservedNodeIndex);
    }

    internal static HkValueVector ReadReferenceVector(TagfileParser tagfileParser, int count)
        => new(Enumerable.Range(0, count).Select(_ => (IHkValue?) ReadReference(tagfileParser)).ToImmutableList());

    internal static HkValueNode ReadStruct(TagfileParser tagfileParser, string? structName) {
        var definition = tagfileParser.Definitions.First(x => x?.Name == structName);
        if (definition is null)
            throw new NullReferenceException();

        var fieldMask = HkBitfield.Read(tagfileParser, definition.NestedFields.Count);
        var values = definition.NestedFields.Zip(fieldMask)
            .Select(x => x.Second ? IHkValue.Read(tagfileParser, x.First.FieldType) : null)
            .ToImmutableList();

        tagfileParser.Nodes.Add(new(definition, values));
        return new(tagfileParser.Nodes, tagfileParser.Nodes.Count - 1);
    }

    internal static HkValueVector ReadStructVector(TagfileParser tagfileParser, string? structName, int count) {
        var definition = tagfileParser.Definitions.First(x => x?.Name == structName);
        if (definition is null)
            throw new NullReferenceException();

        var fieldMask = HkBitfield.Read(tagfileParser, definition.NestedFields.Count);
        var values = definition.NestedFields.Zip(fieldMask)
            .Select(x => x.Second ? IHkValue.ReadVector(tagfileParser, x.First.FieldType, count).Values : null)
            .ToImmutableList();

        return new(Enumerable.Range(0, count).Select(i => {
            tagfileParser.Nodes.Add(new(definition, values.Select(x => x?[i]).ToImmutableList()));
            return (IHkValue?) new HkValueNode(tagfileParser.Nodes, tagfileParser.Nodes.Count - 1);
        }).ToImmutableList());
    }
}
