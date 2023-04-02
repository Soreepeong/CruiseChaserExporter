using System.Numerics;

namespace CruiseChaserExporter.Gltf;

public partial class XivGltfWriter {
    /*
     * https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#coordinate-system-and-units
     * glTF uses a right-handed coordinate system.
     * glTF defines +Y as up, +Z as forward, and -X as right.
     * the front of a glTF asset faces +Z.
     *
     * ...and apparently it's the same with internal XIV format.
     */

    protected static Vector3 NormalizeNormal(Vector3 val) => Vector3.Zero == val ? Vector3.One : Vector3.Normalize(val);
    protected static Vector3 NormalizePosition(Vector4 val) => new(val.X, val.Y, val.Z);
    protected static Vector2 NormalizeUv(Vector4 val) => new(val.X, val.Y);
    protected static Vector3 SwapAxesForScale(Vector3 val) => val;

    protected static Vector4 NormalizeTangent(Vector4 val) {
        var normXyz = Vector3.Normalize(new(val.X, val.Y, val.Z));
        // Tangent W should be 1 or -1, but sometimes XIV has their -1 as 0?
        var w = val.W == 0 ? -1 : val.W;
        return new(normXyz.X, normXyz.Y, normXyz.Z, w);
    }
        
    protected static Quaternion SwapAxesForAnimations(Quaternion val) => val;
    protected static Quaternion SwapAxesForLayout(Quaternion val) => val;
    protected static Matrix4x4 NormalizeTransformationMatrix(Matrix4x4 val) => Matrix4x4.Transpose(val);
}