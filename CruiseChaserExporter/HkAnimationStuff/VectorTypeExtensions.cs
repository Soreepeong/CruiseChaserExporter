using System.Numerics;
using CruiseChaserExporter.Util;

namespace CruiseChaserExporter.HkAnimationStuff;

public static class VectorTypeExtensions {
    public static bool SplineX(this VectorType vt) => 0 != (vt & VectorType.SplineX);
    public static bool SplineY(this VectorType vt) => 0 != (vt & VectorType.SplineY);
    public static bool SplineZ(this VectorType vt) => 0 != (vt & VectorType.SplineZ);
    public static bool Spline(this VectorType vt) => 0 != (vt & VectorType.Spline);
    public static bool StaticX(this VectorType vt) => 0 != (vt & VectorType.StaticX);
    public static bool StaticY(this VectorType vt) => 0 != (vt & VectorType.StaticY);
    public static bool StaticZ(this VectorType vt) => 0 != (vt & VectorType.StaticZ);
    public static bool Static(this VectorType vt) => 0 != (vt & VectorType.Static);

    public static Vector3[] Read(this VectorType vt, BinaryReader reader, int numBlockFrames,
        QuantizationType quantType) {
        if (vt.Spline()) {
            reader.ReadInto(out ushort numItems);
            reader.ReadInto(out byte degree);
            var knots = reader.ReadBytes(numItems + degree + 2);
            reader.AlignTo(4);

            float minx = 0, maxx = 0, miny = 0, maxy = 0, minz = 0, maxz = 0;
            float staticx = 0, staticy = 0, staticz = 0;
            if (vt.SplineX()) {
                reader.ReadInto(out minx);
                reader.ReadInto(out maxx);
            } else if (vt.StaticX()) {
                reader.ReadInto(out staticx);
            }

            if (vt.SplineY()) {
                reader.ReadInto(out miny);
                reader.ReadInto(out maxy);
            } else if (vt.StaticY()) {
                reader.ReadInto(out staticy);
            }

            if (vt.SplineZ()) {
                reader.ReadInto(out minz);
                reader.ReadInto(out maxz);
            } else if (vt.StaticZ()) {
                reader.ReadInto(out staticz);
            }

            var translationControlPoints = new List<Vector3>();
            for (var i = 0; i <= numItems; i++) {
                // yes, "<="
                var position = new Vector3();
                switch (quantType) {
                    case QuantizationType.K8Bit:
                        if (vt.SplineX())
                            position.X = reader.ReadByte() / (float) byte.MaxValue;
                        if (vt.SplineY())
                            position.Y = reader.ReadByte() / (float) byte.MaxValue;
                        if (vt.SplineZ())
                            position.Z = reader.ReadByte() / (float) byte.MaxValue;
                        break;

                    case QuantizationType.K16Bit:
                        if (vt.SplineX())
                            position.X = reader.ReadUInt16() / (float) ushort.MaxValue;
                        if (vt.SplineY())
                            position.Y = reader.ReadUInt16() / (float) ushort.MaxValue;
                        if (vt.SplineZ())
                            position.Z = reader.ReadUInt16() / (float) ushort.MaxValue;
                        break;

                    default:
                        throw new NotSupportedException();
                }

                position.X = vt.SplineX() ? minx + (maxx - minx) * position.X : staticx;
                position.Y = vt.SplineY() ? miny + (maxy - miny) * position.Y : staticy;
                position.Z = vt.SplineZ() ? minz + (maxz - minz) * position.Z : staticz;
                translationControlPoints.Add(position);
            }

            var nurbs = new NurbsVec3(translationControlPoints, knots, degree);
            return Enumerable.Range(0, numBlockFrames).Select(t => nurbs[t]).ToArray();
        } else if (vt.Static()) {
            return new[] {
                new Vector3(
                    vt.StaticX() ? reader.ReadSingle() : 0f,
                    vt.StaticY() ? reader.ReadSingle() : 0f,
                    vt.StaticZ() ? reader.ReadSingle() : 0f
                )
            };
        } else {
            return Array.Empty<Vector3>();
        }
    }
}
