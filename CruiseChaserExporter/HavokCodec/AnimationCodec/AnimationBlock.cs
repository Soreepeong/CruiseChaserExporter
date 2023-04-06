using System.Collections.Immutable;
using CruiseChaserExporter.Animation;
using CruiseChaserExporter.Animation.QuaternionTrack;
using CruiseChaserExporter.Animation.Vector3Track;

namespace CruiseChaserExporter.HavokCodec.AnimationCodec; 

public class AnimationBlock : IAnimation{
    public readonly ImmutableList<AnimationTrack> Tracks;
    public readonly ImmutableDictionary<int, int> BoneToTrack;

    public AnimationBlock(float duration, ImmutableList<AnimationTrack> tracks, ImmutableList<int> transformTrackToBoneIndices) {
        Duration = duration;
        Tracks = tracks;
        BoneToTrack = transformTrackToBoneIndices.Select((v, i) => (v, i)).ToImmutableDictionary(x => x.v, x => x.i);
    }

    public float Duration { get; }

    public ImmutableSortedSet<int> AffectedBoneIndices => BoneToTrack.Keys.ToImmutableSortedSet();
    
    public IVector3Track Translation(int boneIndex) => Tracks[BoneToTrack[boneIndex]].Translate;
    
    public IQuaternionTrack Rotation(int boneIndex) => Tracks[BoneToTrack[boneIndex]].Rotate;
    
    public IVector3Track Scale(int boneIndex) => Tracks[BoneToTrack[boneIndex]].Scale;
}
