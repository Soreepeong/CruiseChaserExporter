namespace CruiseChaserExporter.HkDefinitions;

public class HkaAnimationBinding : HkReferencedObject {
    public string? OriginalSkeletonName;
    public HkaAnimation? Animation;
    public List<int> TransformTrackToBoneIndices;
    public List<int> FloatTrackToFloatSlotIndices;
    public int? BlendHint;
}