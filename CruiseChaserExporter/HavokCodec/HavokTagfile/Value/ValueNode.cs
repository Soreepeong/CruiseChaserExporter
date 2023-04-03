using System.Collections.Immutable;

namespace CruiseChaserExporter.HavokCodec.HavokTagfile.Value;

public class ValueNode : IValue {
    private readonly IList<Node?> _nodes;
    private readonly int _nodeIndex;

    public ValueNode(IList<Node?> nodes, int nodeIndex) {
        _nodes = nodes;
        _nodeIndex = nodeIndex;
    }

    public Node Node => 0 <= _nodeIndex && _nodeIndex < _nodes.Count && _nodes[_nodeIndex] != null
        ? _nodes[_nodeIndex]!
        : throw new NullReferenceException(DescribeNode());

    public override string ToString() => $"Node#{_nodeIndex} ({DescribeNode()})";

    private string DescribeNode() => _nodeIndex < 0 || _nodes.Count <= _nodeIndex
        ? "<out of range>"
        : _nodes[_nodeIndex]?.ToString() ?? "<not yet available>";

    public static implicit operator Node(ValueNode d) => d.Node;

    internal static ValueNode ReadReference(Parser parser) {
        var refIndex = parser.ReadInt();
        if (refIndex < parser.References.Count)
            return new(parser.Nodes, parser.References[refIndex]);

        if (!parser.PendingReferences.TryGetValue(refIndex, out var reservedNodeIndex)) {
            reservedNodeIndex = parser.PendingReferences[refIndex] = parser.Nodes.Count;
            parser.Nodes.Add(null);
        }

        return new(parser.Nodes, reservedNodeIndex);
    }

    internal static ValueArray ReadReferenceVector(Parser parser, int count)
        => new(Enumerable.Range(0, count).Select(_ => (IValue?) ReadReference(parser)).ToImmutableList());

    internal static ValueNode ReadStruct(Parser parser, string? structName) {
        var definition = parser.OrderedDefinitions.First(x => x?.Name == structName);
        if (definition is null)
            throw new NullReferenceException();

        var fieldMask = Bitfield.Read(parser, definition.NestedFields.Count);
        var values = definition.NestedFields.Zip(fieldMask)
            .Select(x => x.Second ? IValue.Read(parser, x.First.FieldType) : null)
            .ToImmutableList();

        parser.Nodes.Add(new(definition, values));
        return new(parser.Nodes, parser.Nodes.Count - 1);
    }

    internal static ValueArray ReadStructVector(Parser parser, string? structName, int count) {
        var definition = parser.OrderedDefinitions.First(x => x?.Name == structName);
        if (definition is null)
            throw new NullReferenceException();

        var fieldMask = Bitfield.Read(parser, definition.NestedFields.Count);
        var values = definition.NestedFields.Zip(fieldMask)
            .Select(x => x.Second ? IValue.ReadVector(parser, x.First.FieldType, count).Values : null)
            .ToImmutableList();

        return new(Enumerable.Range(0, count).Select(i => {
            parser.Nodes.Add(new(definition, values.Select(x => x?[i]).ToImmutableList()));
            return (IValue?) new ValueNode(parser.Nodes, parser.Nodes.Count - 1);
        }).ToImmutableList());
    }
}
