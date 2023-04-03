﻿using System.Numerics;
using CruiseChaserExporter.Util;

namespace CruiseChaserExporter.HavokCodec.AnimationCodec;

public static class QuaternionTypeExtensions {
    public static bool Spline(this QuaternionType rt) => 0 != (rt & QuaternionType.Spline);
    public static bool Static(this QuaternionType rt) => 0 != (rt & QuaternionType.Static);

    public static Quaternion[] Read(this QuaternionType qt, BinaryReader reader, int numBlockFrames,
        QuantizationType quantType) {
        if (qt.Spline()) {
            reader.ReadInto(out ushort numItems);
            reader.ReadInto(out byte degree);
            var knots = reader.ReadBytes(numItems + degree + 2);

            var rotationControlPoints = new List<float[]>();
            for (var i = 0; i <= numItems; ++i) {
                Quaternion rotation;

                switch (quantType) {
                    case QuantizationType.K40Bit:
                        rotation = reader.ReadHk40BitQuaternion();
                        break;
                    default:
                        throw new NotSupportedException();
                }

                rotationControlPoints.Add(new[] {rotation.X, rotation.Y, rotation.Z, rotation.W});
            }

            var nurbs = new Nurbs(4, rotationControlPoints, knots, degree);
            return Enumerable.Range(0, numBlockFrames)
                .Select(t => nurbs[t])
                .Select(x => new Quaternion(x[0], x[1], x[2], x[3]))
                .ToArray();
        } else if (qt.Static()) {
            Quaternion rotation;

            switch (quantType) {
                case QuantizationType.K40Bit:
                    rotation = reader.ReadHk40BitQuaternion();
                    break;
                default:
                    throw new NotSupportedException();
            }

            return new[] {rotation};
        } else {
            return Array.Empty<Quaternion>();
        }
    }
}
