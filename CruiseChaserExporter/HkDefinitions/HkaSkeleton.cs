namespace CruiseChaserExporter.HkDefinitions;

public class HkaSkeleton : HkReferencedObject {
    public string? Name;
    public int[] ParentIndices;
    public HkaBone[] Bones;
    public float[][] ReferencePose;
    public float[] ReferenceFloats;
    public string[] FloatSlots;
    public HkaSkeletonLocalFrameOnBone[] LocalFrames;
}
