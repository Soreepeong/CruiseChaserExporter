namespace CruiseChaserExporter.HkDefinitions;

public class HkxIndexBuffer : HkReferencedObject {
    public int? IndexType;
    public List<int> Indices16;
    public List<int> Indices32;
    public int? VertexBaseOffset;
    public int? Length;
}