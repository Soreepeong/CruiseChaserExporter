namespace CruiseChaserExporter.HkDefinitions;

public class HkxMeshSection : HkReferencedObject {
    public HkxVertexBuffer? VertexBuffer;
    public List<HkxIndexBuffer> IndexBuffers;
    public HkxMaterial? Material;
    public List<HkReferencedObject> UserChannels;
}