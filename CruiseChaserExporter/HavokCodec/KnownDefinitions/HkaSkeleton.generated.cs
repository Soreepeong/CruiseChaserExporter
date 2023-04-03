namespace CruiseChaserExporter.HavokCodec.KnownDefinitions;

#pragma warning disable CS8618
#nullable enable

[System.CodeDom.Compiler.GeneratedCode("CruiseChaserExporter", "1.0.0.0")]
public class HkaSkeleton : HkReferencedObject {
	public string? Name;
	public int[] ParentIndices;
	public HkaBone[] Bones;
	public float[][] ReferencePose;
	public float[] ReferenceFloats;
	public string[] FloatSlots;
	public HkaSkeletonLocalFrameOnBone[] LocalFrames;
}
