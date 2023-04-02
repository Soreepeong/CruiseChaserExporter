namespace CruiseChaserExporter.HkDefinitions;

public class HkxMeshSection : HkReferencedObject {
    public HkxVertexBuffer? VertexBuffer;
    public HkxIndexBuffer[] IndexBuffers;
    public HkxMaterial? Material;
    public HkReferencedObject[] UserChannels;
}