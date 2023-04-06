using System.Collections.Immutable;
using System.Numerics;
using CruiseChaserExporter.Animation.QuaternionTrack;
using CruiseChaserExporter.Animation.Vector3Track;

namespace CruiseChaserExporter.Animation;

public class StaticAnimation : IAnimation {
    private readonly ImmutableDictionary<int, StaticVector3Track> _translations;
    private readonly ImmutableDictionary<int, StaticQuaternionTrack> _rotations;
    private readonly ImmutableDictionary<int, StaticVector3Track> _scales;

    public StaticAnimation(
        IReadOnlyDictionary<int, Vector3> translations,
        IReadOnlyDictionary<int, Quaternion> rotations,
        IReadOnlyDictionary<int, Vector3> scales
    ) {
        AffectedBoneIndices = translations.Keys
            .Concat(rotations.Keys)
            .Concat(scales.Keys)
            .ToImmutableSortedSet();

        _translations = AffectedBoneIndices
            .Select(i => (i, x: translations.GetValueOrDefault(i, Vector3.Zero)))
            .ToImmutableDictionary(
                x => x.i,
                x => new StaticVector3Track(x.x, x.x == Vector3.Zero));
        _rotations = AffectedBoneIndices
            .Select(i => (i, x: rotations.GetValueOrDefault(i, Quaternion.Identity)))
            .ToImmutableDictionary(
                x => x.i,
                x => new StaticQuaternionTrack(x.x));
        _scales = AffectedBoneIndices
            .Select(i => (i, x: scales.GetValueOrDefault(i, Vector3.One)))
            .ToImmutableDictionary(
                x => x.i,
                x => new StaticVector3Track(x.x, x.x == Vector3.One));
    }

    public float Duration => 0f;

    public ImmutableSortedSet<int> AffectedBoneIndices { get; }

    public IVector3Track Translation(int boneIndex) => _translations[boneIndex];

    public IQuaternionTrack Rotation(int boneIndex) => _rotations[boneIndex];

    public IVector3Track Scale(int boneIndex) => _scales[boneIndex];

    private class StaticVector3Track : IVector3Track {
        private readonly Vector3 _value;

        public StaticVector3Track(Vector3 value, bool isEmpty) {
            _value = value;
            IsEmpty = isEmpty;
        }

        public bool IsEmpty { get; }

        public bool IsStatic => true;

        public float Duration => 0f;

        public IEnumerable<float> GetFrameTimes() => Array.Empty<float>();

        public Vector3 Interpolate(float t) => _value;
    }

    private class StaticQuaternionTrack : IQuaternionTrack {
        private readonly Quaternion _value;

        public StaticQuaternionTrack(Quaternion value) {
            _value = value;
        }

        public bool IsEmpty => _value.IsIdentity;

        public bool IsStatic => true;

        public float Duration => 0f;

        public IEnumerable<float> GetFrameTimes() => Array.Empty<float>();

        public Quaternion Interpolate(float t) => _value;
    }
}
