namespace CruiseChaserExporter.HkDefinitions;

public class HkaSplineCompressedAnimation : HkaAnimation {
    public int? NumFrames;
    public int? NumBlocks;
    public int? MaxFramesPerBlock;
    public int? MaskAndQuantizationSize;
    public float? BlockDuration;
    public float? BlockInverseDuration;
    public float? FrameDuration;
    public List<int> BlockOffsets;
    public List<int> FloatBlockOffsets;
    public List<int> TransformOffsets;
    public List<int> FloatOffsets;
    public List<byte> Data;
    public int? Endian;
}