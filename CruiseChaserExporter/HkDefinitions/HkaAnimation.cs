namespace CruiseChaserExporter.HkDefinitions;

public class HkaAnimation : HkReferencedObject {
    public int? Type;
    public float? Duration;
    public int? NumberOfTransformTracks;
    public int? NumberOfFloatTracks;
    public HkaAnimatedReferenceFrame? ExtractedMotion;
    public HkaAnnotationTrack[] AnnotationTracks;
}