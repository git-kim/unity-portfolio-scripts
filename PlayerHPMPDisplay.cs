using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerHPMPDisplay : MonoBehaviour
{
    UIProgressBar[] playerHPMPBars;
    readonly UILabel[] playerHPMPDigitsLabel = new UILabel[2];

    void Start()
    {
        playerHPMPBars = gameObject.GetComponentsInChildren<UIProgressBar>();

        int i = 0;
        foreach (UILabel uILabel
            in gameObject.GetComponentsInChildren<UILabel>().Where(uILabel => uILabel.name.EndsWith("s")))
        {
            playerHPMPDigitsLabel[i++] = uILabel;
        }
    }

    public void UpdateHPBar(int currentHP, int maxHP)
    {
        playerHPMPBars[0].Set((float)currentHP / maxHP, false);
        playerHPMPDigitsLabel[0].text = Mathf.Clamp(currentHP, 0, currentHP).ToString();
    }

    public void UpdateMPBar(int currentMP, int maxMP)
    {
        playerHPMPBars[1].Set((float)currentMP / maxMP, false);
        playerHPMPDigitsLabel[1].text = Mathf.Clamp(currentMP, 0, currentMP).ToString();
    }

}
