using TMPro;
using UnityEngine;

namespace UserInterface
{
    public class HitAndManaPointsDisplay : MonoBehaviour
    {
        [SerializeField] private ProgressBar hitPointsBar;
        [SerializeField] private ProgressBar manaPointsBar;
        [SerializeField] private TMP_Text hitPointsDigitsLabel;
        [SerializeField] private TMP_Text manaPointsDigitsLabel;

        public void UpdateHitPointsBar(int currentPoints, int maximumPoints)
        {
            if (!hitPointsBar)
                return;

            hitPointsBar.Value = (float)currentPoints / maximumPoints;
        }

        public void UpdateHitPointsText(int currentPoints)
        {
            if (!hitPointsDigitsLabel)
                return;

            hitPointsDigitsLabel.text = currentPoints.ToString();
        }

        public void UpdateManaPointsBar(int currentPoints, int maximumPoints)
        {
            if (!manaPointsBar)
                return;

            manaPointsBar.Value = (float)currentPoints / maximumPoints;
        }

        public void UpdateManaPointsText(int currentPoints)
        {
            if (!manaPointsDigitsLabel)
                return;

            manaPointsDigitsLabel.text = currentPoints.ToString();
        }
    }
}