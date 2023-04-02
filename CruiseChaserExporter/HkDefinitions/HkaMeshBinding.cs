namespace CruiseChaserExporter.HkDefinitions;

public class HkaMeshBinding : HkReferencedObject {
    public HkxMesh? Mesh;
    public string? OriginalSkeletonName;
    public string? Name;
    public HkaSkeleton? Skeleton;
    public List<HkaMeshBindingMapping> Mappings;
    public List<float[]> BoneFromSkinMeshTransforms;
}