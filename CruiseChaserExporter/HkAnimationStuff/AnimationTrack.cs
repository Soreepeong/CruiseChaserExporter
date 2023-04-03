using System.Numerics;

namespace CruiseChaserExporter.HkAnimationStuff;

public class AnimationTrack {
    public Vector3[] Translate;
    public Quaternion[] Rotate;
    public Vector3[] Scale;

    public AnimationTrack(Vector3[] translate, Quaternion[] rotate, Vector3[] scale) {
        Translate = translate;
        Rotate = rotate;
        Scale = scale;
    }
}
