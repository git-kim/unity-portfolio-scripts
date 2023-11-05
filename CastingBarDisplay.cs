using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class CastingBarDisplay : MonoBehaviour
{
    abstract public void ShowCastingBar(int actionID, float castTime);
    abstract public void StopShowingCastingBar();
}
