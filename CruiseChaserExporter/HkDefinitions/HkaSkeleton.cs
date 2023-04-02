namespace CruiseChaserExporter.HkDefinitions;

public class HkaSkeleton : HkReferencedObject {
    public string? Name;
    public List<int> ParentIndices;
    public List<HkaBone> Bones;
    public List<float[]> ReferencePose;
    public List<float> ReferenceFloats;
    public List<string> FloatSlots;
    public List<HkaSkeletonLocalFrameOnBone> LocalFrames;
}