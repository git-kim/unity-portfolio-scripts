using UnityEngine;
using UnityEngine.UI;

namespace UserInterface
{
    // Replaces NGUI's UIProgressBar. Filled images use fillAmount; sliced images stretch to keep their 9-slice borders.
    public class ProgressBar : MonoBehaviour
    {
        [SerializeField] private Image foreground;

        public float Value
        {
            get => foreground.type == Image.Type.Filled ? foreground.fillAmount : foreground.rectTransform.anchorMax.x;
            set
            {
                value = Mathf.Clamp01(value);
                if (foreground.type == Image.Type.Filled)
                {
                    foreground.fillAmount = value;
                    return;
                }

                var rect = foreground.rectTransform;
                rect.anchorMax = new Vector2(value, rect.anchorMax.y);
            }
        }
    }
}
