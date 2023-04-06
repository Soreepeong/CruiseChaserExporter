using System.Numerics;

namespace CruiseChaserExporter.Animation.Vector3Track;

public class StaticVector3Track : IVector3Track {
    private readonly Vector3 _value;

    public StaticVector3Track(Vector3 value, float duration, bool isEmpty) {
        _value = value;
        IsEmpty = isEmpty;
        Duration = duration;
    }

    public bool IsEmpty { get; }

    public bool IsStatic => true;

    public float Duration { get; }

    public IEnumerable<float> GetFrameTimes() => new[] {0f};

    public Vector3 Interpolate(float t) => _value;
}
