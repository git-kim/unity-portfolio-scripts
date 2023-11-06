using UnityEngine;

public class PlayerHPMPDisplay : MonoBehaviour
{
    [SerializeField] private UIProgressBar[] playerHPMPBars;
    [SerializeField] private UILabel[] playerHPMPDigitsLabels;

    public void UpdateHPBar(int currentHP, int maxHP)
    {
        playerHPMPBars[0].Set((float)currentHP / maxHP, false);
        playerHPMPDigitsLabels[0].text = Mathf.Clamp(currentHP, 0, currentHP).ToString();
    }

    public void UpdateMPBar(int currentMP, int maxMP)
    {
        playerHPMPBars[1].Set((float)currentMP / maxMP, false);
        playerHPMPDigitsLabels[1].text = Mathf.Clamp(currentMP, 0, currentMP).ToString();
    }
}