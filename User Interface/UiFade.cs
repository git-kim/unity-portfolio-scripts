using System;
using UnityEngine;

namespace UserInterface
{
    // Replaces NGUI's TweenAlpha: plays an alpha curve on a CanvasGroup.
    [RequireComponent(typeof(CanvasGroup))]
    public class UiFade : MonoBehaviour
    {
        [SerializeField] private float duration = 1f;
        [SerializeField] private float from = 1f;
        [SerializeField] private float to;
        [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private bool playOnEnable;

        private CanvasGroup canvasGroup;
        private float elapsed;

        public bool IsPlaying { get; private set; }
        public event Action Finished;

        private CanvasGroup Group => canvasGroup ? canvasGroup : canvasGroup = GetComponent<CanvasGroup>();

        private void OnEnable()
        {
            if (playOnEnable)
                Play();
        }

        public void Play()
        {
            elapsed = 0f;
            IsPlaying = true;
            Group.alpha = from;
        }

        public void ResetToBeginning()
        {
            IsPlaying = false;
            elapsed = 0f;
            Group.alpha = from;
        }

        private void Update()
        {
            if (!IsPlaying)
                return;

            elapsed += Time.deltaTime;
            var t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
            Group.alpha = Mathf.LerpUnclamped(from, to, curve.Evaluate(t));

            if (t < 1f)
                return;

            IsPlaying = false;
            Finished?.Invoke();
        }
    }
}
