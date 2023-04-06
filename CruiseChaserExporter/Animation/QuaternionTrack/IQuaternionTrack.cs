using System.Numerics;

namespace CruiseChaserExporter.Animation.QuaternionTrack;

public interface IQuaternionTrack : ITimeToQuantity {
    Quaternion Interpolate(float t);
}
