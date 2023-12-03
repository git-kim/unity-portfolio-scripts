using System.Collections.Generic;
using UnityEngine;

namespace UserInterface
{
    public class StatChangePrefabController : MonoBehaviour
    {
        private readonly List<TweenAlpha> tweenAlphas = new List<TweenAlpha>();

        private void Awake()
        {
            tweenAlphas.AddRange(GetComponentsInChildren<TweenAlpha>());
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
}