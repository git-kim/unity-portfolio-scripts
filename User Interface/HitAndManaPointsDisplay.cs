using UnityEngine;

public class HitAndManaPointsDisplay : MonoBehaviour
{
    [SerializeField] private UIProgressBar hitPointsBar;
    [SerializeField] private UIProgressBar manaPointsBar;
    [SerializeField] private UILabel hitPointsDigitsLabel;
    [SerializeField] private UILabel manaPointsDigitsLabel;

    public void UpdateHitPointsBar(int currentPoints, int maximumPoints)
    {
        if (!hitPointsBar)
            return;

        hitPointsBar.Set((float)currentPoints / maximumPoints, false);
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

        manaPointsBar.Set((float)currentPoints / maximumPoints, false);
    }

    public void UpdateManaPointsText(int currentPoints)
    {
        if (!manaPointsDigitsLabel)
            return;

        manaPointsDigitsLabel.text = currentPoints.ToString();
    }
}