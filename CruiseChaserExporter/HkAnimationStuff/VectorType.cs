namespace CruiseChaserExporter.HkAnimationStuff;

[Flags]
public enum VectorType : byte {
    StaticX = 0x01,
    StaticY = 0x02,
    StaticZ = 0x04,
    Static = StaticX | StaticY | StaticZ,
    SplineX = 0x10,
    SplineY = 0x20,
    SplineZ = 0x40,
    Spline = SplineX | SplineY | SplineZ,
}
