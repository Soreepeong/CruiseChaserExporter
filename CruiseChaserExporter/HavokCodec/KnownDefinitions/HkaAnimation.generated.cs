namespace CruiseChaserExporter.HavokCodec.KnownDefinitions;

#pragma warning disable CS8618
#nullable enable

[System.CodeDom.Compiler.GeneratedCode("CruiseChaserExporter", "1.0.0.0")]
public class HkaAnimation : HkReferencedObject {
	public int Type;
	public float Duration;
	public int NumberOfTransformTracks;
	public int NumberOfFloatTracks;
	public HkaAnimatedReferenceFrame? ExtractedMotion;
	public HkaAnnotationTrack[] AnnotationTracks;
}
