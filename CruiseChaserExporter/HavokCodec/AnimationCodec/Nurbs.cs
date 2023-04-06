using System.Collections.Immutable;

namespace CruiseChaserExporter.HavokCodec.AnimationCodec;

public class Nurbs {
    private readonly int _elementCount;
    private readonly ImmutableList<float[]> _controlPoints; 
    private readonly ImmutableList<byte> _knots;
    private readonly int _degree;

    public Nurbs(int elementCount, IEnumerable<float[]> controlPoints, IEnumerable<byte> knots, int degree) {
        _elementCount = elementCount;
        _controlPoints = controlPoints.ToImmutableList();
        _knots = knots.ToImmutableList();
        _degree = degree;
    }

    public float[] this[float t] {
        get {
            var span = _FindSpan(t);
            var basis = _BsplineBasis(span, t);

            var value = new float[_elementCount];
            for (var i = 0; i <= _degree; i++) {
                for (var j = 0; j < _elementCount; j++)
                    value[j] += _controlPoints[span - i][j] * basis[i];
            }

            return value;
        }
    }

    /*
     * bsplineBasis and findSpan are based on the implementations of
     * https://github.com/PredatorCZ/HavokLib
     */

    private float[] _BsplineBasis(int span, float t) {
        var res = Enumerable.Range(0, _degree + 1).Select(_ => 0f).ToArray();

        res[0] = 1f;

        for (var i = 0; i < _degree; ++i) {
            for (var j = i; j >= 0; --j) {
                var a = (t - _knots[span - j]) / (_knots[span + i + 1 - j] - _knots[span - j]);
                var tmp = res[j] * a;
                res[j + 1] += res[j] - tmp;
                res[j] = tmp;
            }
        }

        return res;
    }

    private int _FindSpan(float t) {
        if (t >= _knots[_controlPoints.Count])
            return _controlPoints.Count - 1;

        var low = _degree;
        var high = _controlPoints.Count;
        var mid = (low + high) / 2;

        while (t < _knots[mid] || t >= _knots[mid + 1]) {
            if (t < _knots[mid]) {
                high = mid;
            } else {
                low = mid;
            }

            mid = (low + high) / 2;
        }

        return mid;
    }
}
