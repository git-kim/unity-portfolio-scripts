using UnityEngine;

namespace UserInterface
{
    public abstract class CastingBarDisplay : MonoBehaviour
    {
        public abstract void ShowCastingBar(int actionID, float castTime);
        public abstract void StopShowingCastingBar();
    }
}