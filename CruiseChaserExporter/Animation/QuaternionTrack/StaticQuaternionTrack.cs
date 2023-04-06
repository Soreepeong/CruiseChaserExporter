using System.Numerics;

namespace CruiseChaserExporter.Animation.QuaternionTrack;

public class StaticQuaternionTrack : IQuaternionTrack {
    private readonly Quaternion _value;

    public StaticQuaternionTrack(Quaternion value, float duration, bool isEmpty) {
        _value = value;
        IsEmpty = isEmpty;
        Duration = duration;
    }

    public bool IsEmpty { get; }

    public bool IsStatic => true;

    public float Duration { get; }

    public IEnumerable<float> GetFrameTimes() => new[] {0f};

    public Quaternion Interpolate(float t) => _value;
}
