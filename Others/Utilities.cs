using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Utilities
{
    private const float ReciprocalOfPi = 1f / Mathf.PI;

    /// <summary>
    /// Returns the coordinates of a point on a quadratic Bézier curve.
    /// </summary>
    /// <param name="pos0">Coordinates of the first control point</param>
    /// <param name="pos1">Coordinates of the second control point</param>
    /// <param name="pos2">Coordinates of the last control point</param>
    /// <param name="t">A value between 0 (inclusive) and 1 (inclusive)</param>
    /// <returns>Coordinates corresponding to t</returns>
    public static Vector3 GetQuadraticBezierPoint(ref Vector3 pos0, ref Vector3 pos1, ref Vector3 pos2, float t)
    {
        return Mathf.Pow((1f - t), 2f) * pos0 + 2f * (1f - t) * t * pos1 + Mathf.Pow(t, 2f) * pos2;
    }

    /// <summary>
    /// Returns a random float using the quantile function of a sine distribution.
    /// PDF (Probability Density Function): f(x) = (π/2) sin (πx) where 0 ≤ x ≤ 1.
    /// CDF (Cumulative Distribution Function): g(x) = 0.5 * (1 - cos(πx)) where 0 ≤ x ≤ 1.
    /// Quantile function: g^-1(p) = 1/π * arccos(1 - 2p) where 0 ≤ p ≤ 1.
    /// </summary>
    /// <param name="minValue">Interval start value (inclusive)</param>
    /// <param name="maxValue">Interval end value (inclusive)</param>
    /// <returns>A random float between minValue and maxValue</returns>
    public static float GetRandomFloatFromSineDistribution(float minValue, float maxValue)
    {
        return minValue + (maxValue - minValue) * ReciprocalOfPi * Mathf.Acos(1 - 2 * Random.value);
    }

    public static KeyValuePair<TK, TV> GetMaxValuePair<TK, TV>(Dictionary<TK, TV> dictionary)
        where TV : System.IComparable
    {
        KeyValuePair<TK, TV> maxKeyValuePair = dictionary.First();
        foreach (KeyValuePair<TK, TV> keyValuePair in dictionary)
        {
            if (keyValuePair.Value.CompareTo(maxKeyValuePair.Value) > 0)
                maxKeyValuePair = keyValuePair;
        }
        return maxKeyValuePair;
    }

    public static T SelfOrNull<T>(this T target) where T : class
    {
        if (target is Object unityEngineTarget)
        {
            if (!unityEngineTarget)
                return null;
        }

        return target;
    }

    public static void SetActive(this Component component, bool value)
    {
        component.gameObject.SelfOrNull()?.SetActive(value);
    }

    public static bool CheckCylinder(Vector3 start, Vector3 end, float radius,
        int layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        Debug.DrawLine(start, end, Color.red);

        if (!Physics.CheckCapsule(start, end, radius, layerMask, queryTriggerInteraction))
            return false;

        var startToEnd = end - start;
        return Physics.CheckBox(start + 0.5f * startToEnd,
            new Vector3(radius, radius, startToEnd.magnitude * 0.5f),
            Quaternion.LookRotation(startToEnd), layerMask, queryTriggerInteraction);
    }
}