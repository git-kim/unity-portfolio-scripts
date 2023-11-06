using UnityEngine; // Vector3
using System.Collections.Generic;
using System.Linq; // First

public static class Utilities
{
    private const float ReciprocalOfPi = 1f / Mathf.PI;

    /// <summary>
    /// 세 점이 이루는 quadratic Bézier curve에 맞는 점을 구한다.(선형 보간 대신 수식을 사용한다.)
    /// </summary>
    /// <param name="pos0">시작점</param>
    /// <param name="pos1">보간 기준점</param>
    /// <param name="pos2">도착점</param>
    /// <param name="t">범위: 0(포함) ~ 1(포함)</param>
    /// <returns></returns>
    public static Vector3 GetQuadraticBezierPoint(ref Vector3 pos0, ref Vector3 pos1, ref Vector3 pos2, float t)
    {
        // t = Mathf.Clamp(t, 0f, 1f);
        return Mathf.Pow((1f - t), 2f) * pos0 + 2f * (1f - t) * t * pos1 + Mathf.Pow(t, 2f) * pos2;
    }

    /// <summary>
    /// f(x) = (π/2) sin (πx) [x 범위: 0 ~ 1]를 확률 분포 함수(PDF)로 하되 x 범위를 지정 범위만큼으로 하여 임의 값을 반환시킨다.
    /// 이 PDF에 해당하는 CDF(cumulative distribution function)는 g(x) = 0.5 * (1 - cos(πx)) [x 범위: 0 ~ 1]이다.
    /// 이 PDF에 해당하는 quantile function(CDF의 역함수)은 g^-1(p) = 1/π * arccos(1 - 2p) [p 범위: 0 ~ 1]이다.
    /// </summary>
    /// <param name="minValue">범위 시작점 값(포함)</param>
    /// <param name="maxValue">범위 끝점 값(포함)</param>
    /// <returns></returns>
    public static float GetRandomFloatFromSineDistribution(float minValue, float maxValue)
    {
        if (minValue > maxValue)
            (minValue, maxValue) = (maxValue, minValue);

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
}
