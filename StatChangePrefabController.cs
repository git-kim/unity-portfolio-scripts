using System.Collections.Generic;
using UnityEngine;

public class StatChangePrefabController : MonoBehaviour
{
    private readonly List<TweenAlpha> tweenAlphas = new List<TweenAlpha>();

    private void Awake()
    {
        tweenAlphas.AddRange(gameObject.GetComponentsInChildren<TweenAlpha>());

        tweenAlphas[0].AddOnFinished(() => gameObject.SetActive(false));
    }

    private void OnEnable()
    {
        foreach (var tweenAlpha in tweenAlphas)
        {
            tweenAlpha.ResetToBeginning();
            tweenAlpha.PlayForward();
        }
    }
}