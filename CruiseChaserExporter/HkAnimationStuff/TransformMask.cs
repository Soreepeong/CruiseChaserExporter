using CruiseChaserExporter.Util;

namespace CruiseChaserExporter.HkAnimationStuff;

public class TransformMask {
    public byte Quantization;
    public VectorType Translate;
    public QuaternionType Rotate;
    public VectorType Scale;

    public TransformMask(BinaryReader reader) {
        reader.ReadInto(out Quantization);
        reader.ReadInto(out Translate);
        reader.ReadInto(out Rotate);
        reader.ReadInto(out Scale);
    }

    public QuantizationType TranslateQuantization {
        get => (QuantizationType) (Quantization & 0b11);
        set => Quantization = (byte) ((Quantization & 0b11111100) | (int) value);
    }

    public QuantizationType RotateQuantization {
        get => (QuantizationType) (((Quantization >> 2) & 0b1111) + 2);
        set => Quantization = (byte) ((Quantization & 0b11000011) | ((int) (value - 2) << 2));
    }

    public QuantizationType ScaleQuantization {
        get => (QuantizationType) ((Quantization >> 6) & 0b11);
        set => Quantization = (byte) ((Quantization & 0b00111111) | ((int) value << 6));
    }
}