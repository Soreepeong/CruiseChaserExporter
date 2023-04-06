using CruiseChaserExporter.Util;

namespace CruiseChaserExporter.HavokCodec.AnimationCodec;

public class TransformMask {
    public byte Quantization;
    public VectorType Translation;
    public QuaternionType Rotation;
    public VectorType Scale;

    public TransformMask(BinaryReader reader) {
        reader.ReadInto(out Quantization);
        reader.ReadInto(out Translation);
        reader.ReadInto(out Rotation);
        reader.ReadInto(out Scale);
    }

    public ScalarQuantization TranslationQuantization {
        get => (ScalarQuantization) (Quantization & 0b11);
        set => Quantization = (byte) ((Quantization & 0b11111100) | (int) value);
    }

    public QuaternionQuantization RotationQuantization {
        get => (QuaternionQuantization) ((Quantization >> 2) & 0b1111);
        set => Quantization = (byte) ((Quantization & 0b11000011) | ((int) value << 2));
    }

    public ScalarQuantization ScaleQuantization {
        get => (ScalarQuantization) ((Quantization >> 6) & 0b11);
        set => Quantization = (byte) ((Quantization & 0b00111111) | ((int) value << 6));
    }
}
