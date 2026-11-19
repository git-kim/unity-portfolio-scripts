using TMPro;
using UnityEngine;

namespace UserInterface
{
    /// <summary>Cast bar shown under an enemy's HP bar while it channels a skill.</summary>
    public class EnemyCastingBarDisplay : CastingBarDisplay
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private ProgressBar bar;
        [SerializeField] private TMP_Text actionNameLabel;

        private float castTime;
        private float elapsed;
        private bool isShown;

        private void Awake()
        {
            group.alpha = 0f;
        }

        public void Show(string actionName, float castTime)
        {
            actionNameLabel.text = actionName;
            ShowCastingBar(0, castTime);
        }

        public override void ShowCastingBar(int actionID, float castTime)
        {
            this.castTime = castTime;
            elapsed = 0f;
            bar.Value = 0f;
            group.alpha = 1f;
            isShown = true;
        }

        public override void StopShowingCastingBar()
        {
            isShown = false;
            group.alpha = 0f;
        }

        private void Update()
        {
            if (!isShown)
                return;

            elapsed += Time.deltaTime;
            bar.Value = castTime > 0f ? elapsed / castTime : 1f;
        }
    }
}
