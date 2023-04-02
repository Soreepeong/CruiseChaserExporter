using System.Collections.Immutable;
using System.Numerics;

namespace CruiseChaserExporter.HkAnimationStuff;

public class NurbsQuat {
    private readonly ImmutableList<Quaternion> _controlPoints;
    private readonly ImmutableList<byte> _knots;
    private readonly int _degree;

    public NurbsQuat(IEnumerable<Quaternion> controlPoints, IEnumerable<byte> knots, int degree) {
        _controlPoints = controlPoints.ToImmutableList();
        _knots = knots.ToImmutableList();
        _degree = degree;
    }

    public Quaternion this[int t] {
        get {
            var span = _findSpan(t);
            var basis = _bsplineBasis(span, t);

            var value = new Quaternion();
            for (var i = 0; i <= _degree; i++)
                value += _controlPoints[span - i] * basis[i];

            return Quaternion.Normalize(value);
        }
    }

    /*
     * bsplineBasis and findSpan are based on the implementations of
     * https://github.com/PredatorCZ/HavokLib
     */

    private float[] _bsplineBasis(int span, float t) {
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

    private int _findSpan(int t) {
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