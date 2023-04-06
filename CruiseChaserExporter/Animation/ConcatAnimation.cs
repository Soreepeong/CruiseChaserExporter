using System.Collections.Immutable;
using System.Numerics;
using CruiseChaserExporter.Animation.QuaternionTrack;
using CruiseChaserExporter.Animation.Vector3Track;

namespace CruiseChaserExporter.Animation;

public class ConcatAnimation : IAnimation {
    public readonly ImmutableList<IAnimation> Parts;
    private readonly ImmutableDictionary<int, ConcatTranslationTrack> _concatTranslationTracks;
    private readonly ImmutableDictionary<int, ConcatRotationTrack> _concatRotationTracks;
    private readonly ImmutableDictionary<int, ConcatScaleTrack> _concatScaleTracks;

    public ConcatAnimation(IEnumerable<IAnimation> animations) {
        Parts = animations.ToImmutableList();
        AffectedBoneIndices = Parts.SelectMany(x => x.AffectedBoneIndices).ToImmutableSortedSet();
        _concatTranslationTracks = AffectedBoneIndices.ToImmutableDictionary(
            x => x, x => new ConcatTranslationTrack(this, x));
        _concatRotationTracks = AffectedBoneIndices.ToImmutableDictionary(
            x => x, x => new ConcatRotationTrack(this, x));
        _concatScaleTracks = AffectedBoneIndices.ToImmutableDictionary(
            x => x, x => new ConcatScaleTrack(this, x));
    }

    public float Duration => Parts.Select(x => x.Duration).Sum();

    public ImmutableSortedSet<int> AffectedBoneIndices { get; }

    public IVector3Track Translation(int boneIndex) => _concatTranslationTracks[boneIndex];

    public IQuaternionTrack Rotation(int boneIndex) => _concatRotationTracks[boneIndex];

    public IVector3Track Scale(int boneIndex) => _concatScaleTracks[boneIndex];

    private class ConcatTranslationTrack : IVector3Track {
        private readonly ConcatAnimation _parent;
        private readonly IVector3Track[] _parts;

        internal ConcatTranslationTrack(ConcatAnimation parent, int boneIndex) {
            _parent = parent;
            _parts = _parent.Parts.Select(x => x.Translation(boneIndex)).ToArray();
        }

        public bool IsEmpty => _parts.All(x => x.IsEmpty);

        public bool IsStatic => _parts.All(x => x.IsStatic) &&
                                _parts.Zip(_parts.Skip(1))
                                    .All((x) => x.First.Interpolate(0) == x.Second.Interpolate(0));

        public float Duration => _parent.Duration;

        public IEnumerable<float> GetFrameTimes() => ConcatFrameTimes(_parts);

        public Vector3 Interpolate(float t) {
            t %= _parent.Duration;
            while (true) {
                for (var i = 0; i < _parts.Length; i++) {
                    var d = _parent.Parts[i].Duration;
                    if (t < d)
                        return _parts[i].Interpolate(t);
                    t -= d;
                }
            }
        }
    }

    private class ConcatRotationTrack : IQuaternionTrack {
        private readonly ConcatAnimation _parent;
        private readonly IQuaternionTrack[] _parts;

        internal ConcatRotationTrack(ConcatAnimation parent, int boneIndex) {
            _parent = parent;
            _parts = _parent.Parts.Select(x => x.Rotation(boneIndex)).ToArray();
        }

        public bool IsEmpty => _parts.All(x => x.IsEmpty);

        public bool IsStatic => _parts.All(x => x.IsStatic) &&
                                _parts.Zip(_parts.Skip(1))
                                    .All((x) => x.First.Interpolate(0) == x.Second.Interpolate(0));

        public float Duration => _parent.Duration;

        public IEnumerable<float> GetFrameTimes() => ConcatFrameTimes(_parts);

        public Quaternion Interpolate(float t) {
            t %= _parent.Duration;
            while (true) {
                for (var i = 0; i < _parts.Length; i++) {
                    var d = _parent.Parts[i].Duration;
                    if (t < d)
                        return _parts[i].Interpolate(t);
                    t -= d;
                }
            }
        }
    }

    private class ConcatScaleTrack : IVector3Track {
        private readonly ConcatAnimation _parent;
        private readonly IVector3Track[] _parts;

        internal ConcatScaleTrack(ConcatAnimation parent, int boneIndex) {
            _parent = parent;
            _parts = _parent.Parts.Select(x => x.Scale(boneIndex)).ToArray();
        }

        public bool IsEmpty => _parts.All(x => x.IsEmpty);

        public bool IsStatic => _parts.All(x => x.IsStatic) &&
                                _parts.Zip(_parts.Skip(1))
                                    .All(x => x.First.Interpolate(0) == x.Second.Interpolate(0));

        public float Duration => _parent.Duration;

        public IEnumerable<float> GetFrameTimes() => ConcatFrameTimes(_parts);

        public Vector3 Interpolate(float t) {
            t %= _parent.Duration;
            while (true) {
                for (var i = 0; i < _parts.Length; i++) {
                    var d = _parent.Parts[i].Duration;
                    if (t < d)
                        return _parts[i].Interpolate(t);
                    t -= d;
                }
            }
        }
    }

    private static IEnumerable<float> ConcatFrameTimes(IEnumerable<ITimeToQuantity> items) {
        var res = Enumerable.Repeat(0f, 0);
        var baseTime = 0f;
        foreach (var s in items) {
            var baseTimeCopy = baseTime;
            res = res.Concat(s.GetFrameTimes().Select(x => baseTimeCopy + x));
            baseTime += s.Duration;
        }

        return res;
    }
}
