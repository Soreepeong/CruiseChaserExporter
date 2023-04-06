using CruiseChaserExporter.Animation.QuaternionTrack;
using CruiseChaserExporter.Animation.Vector3Track;

namespace CruiseChaserExporter.HavokCodec.AnimationCodec;

public class AnimationTrack {
    public readonly IVector3Track Translate;
    public readonly IQuaternionTrack Rotate;
    public readonly IVector3Track Scale;

    public AnimationTrack(IVector3Track translate, IQuaternionTrack rotate, IVector3Track scale) {
        Translate = translate;
        Rotate = rotate;
        Scale = scale;
    }

    public bool IsEmpty => Translate.IsEmpty && Rotate.IsEmpty && Scale.IsEmpty;
}
