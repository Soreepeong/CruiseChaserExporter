namespace CruiseChaserExporter.HavokCodec.KnownDefinitions;

#pragma warning disable CS8618
#nullable enable

[System.CodeDom.Compiler.GeneratedCode("CruiseChaserExporter", "1.0.0.0")]
public class HkaSplineCompressedAnimation : HkaAnimation {
	public int NumFrames;
	public int NumBlocks;
	public int MaxFramesPerBlock;
	public int MaskAndQuantizationSize;
	public float BlockDuration;
	public float BlockInverseDuration;
	public float FrameDuration;
	public int[] BlockOffsets;
	public int[] FloatBlockOffsets;
	public int[] TransformOffsets;
	public int[] FloatOffsets;
	public byte[] Data;
	public int Endian;
}
