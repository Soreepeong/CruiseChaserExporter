namespace CruiseChaserExporter.HavokCodec.AnimationCodec;

[Flags]
public enum QuaternionType : byte {
    Spline = 0xF0,
    Static = 0x0F,
}
