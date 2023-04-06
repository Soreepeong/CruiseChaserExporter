using System.Collections.Immutable;
using CruiseChaserExporter.Animation.QuaternionTrack;
using CruiseChaserExporter.Animation.Vector3Track;

namespace CruiseChaserExporter.Animation;

public interface IAnimation {
    float Duration { get; }

    ImmutableSortedSet<int> AffectedBoneIndices { get; }

    IVector3Track Translation(int boneIndex);

    IQuaternionTrack Rotation(int boneIndex);

    IVector3Track Scale(int boneIndex);
}
