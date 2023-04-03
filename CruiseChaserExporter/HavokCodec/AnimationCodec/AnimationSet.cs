using System.Collections.Immutable;
using CruiseChaserExporter.HavokCodec.KnownDefinitions;
using CruiseChaserExporter.Util;

namespace CruiseChaserExporter.HavokCodec.AnimationCodec;

public class AnimationSet {
    public readonly ImmutableList<ImmutableList<AnimationTrack>> TrackBlocks;
    public readonly float Duration;
    public readonly float BlockDuration;
    public readonly float FrameDuration;

    public AnimationSet(
        ImmutableList<ImmutableList<AnimationTrack>> trackBlocks,
        float duration,
        float blockDuration,
        float frameDuration) {
        TrackBlocks = trackBlocks;
        Duration = duration;
        BlockDuration = blockDuration;
        FrameDuration = frameDuration;
    }

    public static AnimationSet Decode(HkaAnimation animation) {
        if (animation is HkaSplineCompressedAnimation sca)
            return Decode(sca);

        throw new NotSupportedException();
    }

    public static AnimationSet Decode(HkaSplineCompressedAnimation sca) {
        var res = new List<ImmutableList<AnimationTrack>>();
        var numPendingFrames = sca.NumFrames;
        foreach (var blockOffset in sca.BlockOffsets) {
            using var reader = new BinaryReader(new MemoryStream(sca.Data, blockOffset, sca.Data.Length - blockOffset));

            var masks = Enumerable.Range(0, sca.NumberOfTransformTracks)
                .Select(_ => new TransformMask(reader))
                .ToArray();

            var numBlockFrames = Math.Min(numPendingFrames, sca.MaxFramesPerBlock);
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

        return new(res.ToImmutableList(), sca.Duration, sca.BlockDuration, sca.FrameDuration);
    }
}
