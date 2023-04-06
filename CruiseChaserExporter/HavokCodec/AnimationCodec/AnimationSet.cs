using System.Collections.Immutable;
using System.Numerics;
using CruiseChaserExporter.Animation;
using CruiseChaserExporter.Animation.QuaternionTrack;
using CruiseChaserExporter.Animation.Vector3Track;
using CruiseChaserExporter.HavokCodec.KnownDefinitions;
using CruiseChaserExporter.Util;

namespace CruiseChaserExporter.HavokCodec.AnimationCodec;

public class AnimationSet : IAnimation {
    public readonly ImmutableList<AnimationBlock> Blocks;
    public readonly float BlockDuration;
    public readonly float FrameDuration;

    private readonly ConcatAnimation _concatAnimation;

    public AnimationSet(
        ImmutableList<AnimationBlock> blocks,
        float duration,
        float blockDuration,
        float frameDuration) {
        Blocks = blocks;
        BlockDuration = blockDuration;
        FrameDuration = frameDuration;
        Duration = duration;
        _concatAnimation = new(blocks);
    }

    public float Duration { get; }
    public ImmutableSortedSet<int> AffectedBoneIndices => _concatAnimation.AffectedBoneIndices;
    public IVector3Track Translation(int boneIndex) => _concatAnimation.Translation(boneIndex);
    public IQuaternionTrack Rotation(int boneIndex) => _concatAnimation.Rotation(boneIndex);
    public IVector3Track Scale(int boneIndex) => _concatAnimation.Scale(boneIndex);

    public static AnimationSet Decode(HkaAnimationBinding animationBinding) {
        if (animationBinding.Animation is HkaSplineCompressedAnimation sca)
            return Decode(sca, animationBinding.TransformTrackToBoneIndices.ToImmutableList());

        throw new NotSupportedException();
    }

    public static AnimationSet Decode(HkaSplineCompressedAnimation sca,
        ImmutableList<int> transformTrackToBoneIndices) {
        var res = new List<AnimationBlock>();
        var numPendingFrames = sca.NumFrames;
        var pendingDuration = sca.Duration;
        foreach (var blockOffset in sca.BlockOffsets) {
            using var reader = new BinaryReader(new MemoryStream(sca.Data, blockOffset, sca.Data.Length - blockOffset));

            var masks = Enumerable.Range(0, sca.NumberOfTransformTracks)
                .Select(_ => new TransformMask(reader))
                .ToArray();

            var numBlockFrames = Math.Min(numPendingFrames, sca.MaxFramesPerBlock);
            var blockDuration = Math.Min(pendingDuration, sca.BlockDuration);
            numPendingFrames -= numBlockFrames;
            pendingDuration -= blockDuration;
            var tracks = new List<AnimationTrack>();
            foreach (var mask in masks) {
                var translations = VectorTrackFromSplineData(reader, mask.Translation, mask.TranslationQuantization,
                    numBlockFrames, sca.FrameDuration, blockDuration, false);
                reader.AlignTo(4);

                var rotations = QuaternionTrackFromSplineData(reader, mask.Rotation, mask.RotationQuantization,
                    numBlockFrames, sca.FrameDuration, blockDuration);
                reader.AlignTo(4);

                var scales = VectorTrackFromSplineData(reader, mask.Scale, mask.ScaleQuantization, numBlockFrames,
                    sca.FrameDuration, blockDuration, true);
                reader.AlignTo(4);

                tracks.Add(new(translations, rotations, scales));
            }

            res.Add(new(blockDuration, tracks.ToImmutableList(), transformTrackToBoneIndices));
        }

        return new(res.ToImmutableList(), sca.Duration, sca.BlockDuration, sca.FrameDuration);
    }

    public static IVector3Track VectorTrackFromSplineData(
        BinaryReader reader,
        VectorType vt,
        ScalarQuantization quantType,
        int numFrames,
        float frameDuration,
        float blockDuration,
        bool isScale) {
        if (vt.Spline()) {
            reader.ReadInto(out ushort numItems);
            reader.ReadInto(out byte degree);
            var knots = reader.ReadBytes(numItems + degree + 2);
            reader.AlignTo(4);

            float minx = 0, maxx = 0, miny = 0, maxy = 0, minz = 0, maxz = 0;
            float staticx = 0, staticy = 0, staticz = 0;
            if (vt.SplineX()) {
                reader.ReadInto(out minx);
                reader.ReadInto(out maxx);
            } else if (vt.StaticX()) {
                reader.ReadInto(out staticx);
            }

            if (vt.SplineY()) {
                reader.ReadInto(out miny);
                reader.ReadInto(out maxy);
            } else if (vt.StaticY()) {
                reader.ReadInto(out staticy);
            }

            if (vt.SplineZ()) {
                reader.ReadInto(out minz);
                reader.ReadInto(out maxz);
            } else if (vt.StaticZ()) {
                reader.ReadInto(out staticz);
            }

            var translationControlPoints = new List<float[]>();
            for (var i = 0; i <= numItems; i++) {
                // yes, "<="
                var position = new float[3];
                switch (quantType) {
                    case ScalarQuantization.K8Bit:
                        if (vt.SplineX())
                            position[0] = reader.ReadByte() / (float) byte.MaxValue;
                        if (vt.SplineY())
                            position[1] = reader.ReadByte() / (float) byte.MaxValue;
                        if (vt.SplineZ())
                            position[2] = reader.ReadByte() / (float) byte.MaxValue;
                        break;

                    case ScalarQuantization.K16Bit:
                        if (vt.SplineX())
                            position[0] = reader.ReadUInt16() / (float) ushort.MaxValue;
                        if (vt.SplineY())
                            position[1] = reader.ReadUInt16() / (float) ushort.MaxValue;
                        if (vt.SplineZ())
                            position[2] = reader.ReadUInt16() / (float) ushort.MaxValue;
                        break;

                    default:
                        throw new NotSupportedException();
                }

                position[0] = vt.SplineX() ? minx + (maxx - minx) * position[0] : staticx;
                position[1] = vt.SplineY() ? miny + (maxy - miny) * position[1] : staticy;
                position[2] = vt.SplineZ() ? minz + (maxz - minz) * position[2] : staticz;
                translationControlPoints.Add(position);
            }

            return new SplineVector3Track(new(3, translationControlPoints, knots, degree), blockDuration, numFrames - 1,
                frameDuration);
        }

        if (vt.Static()) {
            return new StaticVector3Track(new(
                vt.StaticX() ? reader.ReadSingle() : (isScale ? 1f : 0f),
                vt.StaticY() ? reader.ReadSingle() : (isScale ? 1f : 0f),
                vt.StaticZ() ? reader.ReadSingle() : (isScale ? 1f : 0f)
            ), blockDuration, false);
        }

        return new StaticVector3Track(
            isScale ? Vector3.One : Vector3.Zero,
            blockDuration,
            true);
    }

    public static IQuaternionTrack QuaternionTrackFromSplineData(
        BinaryReader reader,
        QuaternionType qt,
        QuaternionQuantization quantType,
        int numFrames,
        float frameDuration,
        float blockDuration) {
        if (qt.Spline()) {
            reader.ReadInto(out ushort numItems);
            reader.ReadInto(out byte degree);
            var knots = reader.ReadBytes(numItems + degree + 2);
            
            reader.AlignTo(quantType switch {
                QuaternionQuantization.Quat32 => 4,
                QuaternionQuantization.Quat48 => 2,
                _ => 1
            });

            var rotationControlPoints = new List<float[]>();
            for (var i = 0; i <= numItems; ++i) {
                var rotation = quantType switch {
                    QuaternionQuantization.Quat32 => reader.ReadHk32BitQuaternion(),
                    QuaternionQuantization.Quat40 => reader.ReadHk40BitQuaternion(),
                    QuaternionQuantization.Quat48 => reader.ReadHk48BitQuaternion(),
                    _ => throw new NotSupportedException()
                };

                rotationControlPoints.Add(new[] {rotation.X, rotation.Y, rotation.Z, rotation.W});
            }

            return new SplineQuaternionTrack(new(4, rotationControlPoints, knots, degree), blockDuration, numFrames - 1,
                frameDuration);
        }

        if (qt.Static()) {
            return new StaticQuaternionTrack(quantType switch {
                    QuaternionQuantization.Quat32 => reader.ReadHk32BitQuaternion(),
                    QuaternionQuantization.Quat40 => reader.ReadHk40BitQuaternion(),
                    QuaternionQuantization.Quat48 => reader.ReadHk48BitQuaternion(),
                    _ => throw new NotSupportedException()
                }, blockDuration,
                false);
        }

        return new StaticQuaternionTrack(Quaternion.Identity, blockDuration, true);
    }
}
