namespace CruiseChaserExporter.HkDefinitions;

public class HkaBoneAttachment : HkReferencedObject {
    public string? OriginalSkeletonName;
    public float[] BoneFromAttachment;
    public HkReferencedObject? Attachment;
    public string? Name;
    public int? BoneIndex;
}