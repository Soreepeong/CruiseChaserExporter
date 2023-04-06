namespace CruiseChaserExporter.HavokCodec.AnimationCodec;

public static class VectorTypeExtensions {
    public static bool SplineX(this VectorType vt) => 0 != (vt & VectorType.SplineX);
    public static bool SplineY(this VectorType vt) => 0 != (vt & VectorType.SplineY);
    public static bool SplineZ(this VectorType vt) => 0 != (vt & VectorType.SplineZ);
    public static bool Spline(this VectorType vt) => 0 != (vt & VectorType.Spline);
    public static bool StaticX(this VectorType vt) => 0 != (vt & VectorType.StaticX);
    public static bool StaticY(this VectorType vt) => 0 != (vt & VectorType.StaticY);
    public static bool StaticZ(this VectorType vt) => 0 != (vt & VectorType.StaticZ);
    public static bool Static(this VectorType vt) => 0 != (vt & VectorType.Static);
}
