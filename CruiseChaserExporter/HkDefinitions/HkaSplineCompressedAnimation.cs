namespace CruiseChaserExporter.HkDefinitions;

public class HkaSplineCompressedAnimation : HkaAnimation {
    public int? NumFrames;
    public int? NumBlocks;
    public int? MaxFramesPerBlock;
    public int? MaskAndQuantizationSize;
    public float? BlockDuration;
    public float? BlockInverseDuration;
    public float? FrameDuration;
    public int[] BlockOffsets;
    public int[] FloatBlockOffsets;
    public int[] TransformOffsets;
    public int[] FloatOffsets;
    public byte[] Data;
    public int? Endian;
}