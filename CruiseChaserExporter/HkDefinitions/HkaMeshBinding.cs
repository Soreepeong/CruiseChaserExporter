namespace CruiseChaserExporter.HkDefinitions;

public class HkaMeshBinding : HkReferencedObject {
    public HkxMesh? Mesh;
    public string? OriginalSkeletonName;
    public string? Name;
    public HkaSkeleton? Skeleton;
    public HkaMeshBindingMapping[] Mappings;
    public float[][] BoneFromSkinMeshTransforms;
}