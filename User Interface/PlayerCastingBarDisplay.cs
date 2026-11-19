using Characters;
using Characters.Handlers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UserInterface
{
    public class PlayerCastingBarDisplay : CastingBarDisplay
    {
        [SerializeField] private CanvasGroup castingBarPanel;
        [SerializeField] private Image castingBarActionIcon;
        [SerializeField] private ProgressBar castingBar;
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private UiFade timeLabelFade;

        private CharacterActionHandler playerActionHandler;
        private ActionButtons actionButtons;
        private bool shouldShowCastingBar = false;
        public bool IsShown => Mathf.Approximately(castingBarPanel.alpha, 1f);
        private float castTime, remainingCastTime;

        private void Start()
        {
            actionButtons = FindAnyObjectByType<ActionButtons>();
            playerActionHandler = FindAnyObjectByType<Player>().GetComponent<CharacterActionHandler>();
            castingBarPanel.alpha = 0f;
        }

        public override void ShowCastingBar(int actionID, float castTime)
        {
            var actionIcon = actionButtons.GetActionIcon(actionID);
            castingBarActionIcon.sprite = actionIcon.sprite;
            castingBarActionIcon.color = actionIcon.color;

            this.castTime = remainingCastTime = castTime;
            castingBar.Value = 1f;
            timeLabel.text = playerActionHandler.VisibleGlobalCoolDownTime.ToString("0.0");

            timeLabelFade.ResetToBeginning();

            shouldShowCastingBar = true;
            castingBarPanel.alpha = 1f;
        }

        public override void StopShowingCastingBar()
        {
            timeLabel.text = "중단";
            timeLabelFade.Play();
        }

        private void HideCastingBar()
        {
            castTime = 0f;
            shouldShowCastingBar = false;
            castingBarPanel.alpha = 0f;
        }

        private void Update()
        {
            if (!shouldShowCastingBar) return;
            if (!timeLabelFade.IsPlaying)
            {
                if (!playerActionHandler.IsCasting)
                {
                    HideCastingBar();
                }
                else
                {
                    remainingCastTime = Mathf.Max(0f, remainingCastTime - Time.deltaTime);
                    castingBar.Value = (castTime - remainingCastTime) / castTime;
                }

                timeLabel.text = playerActionHandler.VisibleGlobalCoolDownTime.ToString("0.0");
            }
            else castingBar.Value = 0f;
        }
    }
}
