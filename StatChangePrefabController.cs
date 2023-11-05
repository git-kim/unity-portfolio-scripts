using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatChangePrefabController : MonoBehaviour
{
    List<TweenAlpha> tweenAlphas = new List<TweenAlpha>();

    void Awake()
    {
        tweenAlphas.AddRange(gameObject.GetComponentsInChildren<TweenAlpha>());

        tweenAlphas[0].AddOnFinished(() => gameObject.SetActive(false));
    }
    void OnEnable()
    {
        foreach (TweenAlpha tweenAlpha in tweenAlphas)
        {
            tweenAlpha.ResetToBeginning();
            tweenAlpha.PlayForward();
        }
    }
}
