using System.Collections.Immutable;
using System.Numerics;
using CruiseChaserExporter.HkDefinitions;
using CruiseChaserExporter.Util;

namespace CruiseChaserExporter.HkAnimationStuff;

public class HkAnimationDecoder {
    public readonly ImmutableList<ImmutableList<AnimationTrack>> TrackBlocks;
    public readonly float Duration;
    public readonly float BlockDuration;
    public readonly float FrameDuration;

    public HkAnimationDecoder(
        ImmutableList<ImmutableList<AnimationTrack>> trackBlocks,
        float duration,
        float blockDuration,
        float frameDuration) {
        TrackBlocks = trackBlocks;
        Duration = duration;
        BlockDuration = blockDuration;
        FrameDuration = frameDuration;
    }

    public static HkAnimationDecoder Decode(HkaAnimation animation) {
        if (animation is HkaSplineCompressedAnimation sca)
            return Decode(sca);

        throw new NotSupportedException();
    }

    public static HkAnimationDecoder Decode(HkaSplineCompressedAnimation sca) {
        var res = new List<ImmutableList<AnimationTrack>>();
        var numPendingFrames = sca.NumFrames ?? 0;
        foreach (var blockOffset in sca.BlockOffsets) {
            using var reader = new BinaryReader(new MemoryStream(sca.Data, blockOffset, sca.Data.Length - blockOffset));

            var masks = Enumerable.Range(0, sca.NumberOfTransformTracks ?? 0)
                .Select(_ => new TransformMask(reader))
                .ToArray();

            var numBlockFrames = Math.Min(numPendingFrames, sca.MaxFramesPerBlock ?? 0);
            numPendingFrames -= numBlockFrames;
            var tracks = new List<AnimationTrack>();
            foreach (var mask in masks) {
                var translations = mask.Translate.Read(reader, numBlockFrames, mask.TranslateQuantization);
                reader.AlignTo(4);

                var rotations = mask.Rotate.Read(reader, numBlockFrames, mask.RotateQuantization);
                reader.AlignTo(4);

                var scales = mask.Scale.Read(reader, numBlockFrames, mask.ScaleQuantization);
                reader.AlignTo(4);

                tracks.Add(new(translations, rotations, scales));
            }

            res.Add(tracks.ToImmutableList());
        }

        return new(res.ToImmutableList(), sca.Duration!.Value, sca.BlockDuration!.Value, sca.FrameDuration!.Value);
    }
}
