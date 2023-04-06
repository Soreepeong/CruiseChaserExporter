namespace CruiseChaserExporter.HavokCodec.AnimationCodec;

public static class QuaternionTypeExtensions {
    public static bool Spline(this QuaternionType rt) => 0 != (rt & QuaternionType.Spline);
    public static bool Static(this QuaternionType rt) => 0 != (rt & QuaternionType.Static);
}
