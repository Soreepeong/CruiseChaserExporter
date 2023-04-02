namespace CruiseChaserExporter.HkDefinitions;

public class HkxIndexBuffer : HkReferencedObject {
    public int? IndexType;
    public int[] Indices16;
    public int[] Indices32;
    public int? VertexBaseOffset;
    public int? Length;
}