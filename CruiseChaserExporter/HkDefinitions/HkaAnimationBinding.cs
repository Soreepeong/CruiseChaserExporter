namespace CruiseChaserExporter.HkDefinitions;

public class HkaAnimationBinding : HkReferencedObject {
    public string? OriginalSkeletonName;
    public HkaAnimation? Animation;
    public int[] TransformTrackToBoneIndices;
    public int[] FloatTrackToFloatSlotIndices;
    public int? BlendHint;
}
