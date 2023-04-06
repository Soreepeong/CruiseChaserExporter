using System.Numerics;

namespace CruiseChaserExporter.Animation.Vector3Track;

public interface IVector3Track : ITimeToQuantity {
    Vector3 Interpolate(float t);
}
