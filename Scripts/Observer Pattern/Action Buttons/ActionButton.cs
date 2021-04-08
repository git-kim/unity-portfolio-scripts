using UnityEngine;
using ObserverPattern;

abstract public class ActionButton : MonoBehaviour, IObserver
{
    public int actionID;

    #region 클래스(하위 클래스 포함) 공유 변수
    static protected bool HasPlayerVariablesBeenSet = false;
    static protected Player player;
    static protected Actions playerActionCommands;
    static protected Color usablenessColor = new Color(0x00 / 255f, 0x6D / 255f, 0xA4 / 255f);
    static protected Color unusablenessColor = new Color(0xA4 / 255f, 0x00 / 255f, 0x03 / 255f);
    #endregion

    protected void SetPlayerVariables()
    {
        if (HasPlayerVariablesBeenSet) return;
        HasPlayerVariablesBeenSet = true;
        player = FindObjectOfType<Player>();
        playerActionCommands = player.ActionCommands;
    }

    // OnPress 트리거 발생 시 호출되는 함수
    public void OnPress(bool state)
    {
        if (state) // state가 true일 때
            player.recentActionInput = actionID;
    }

    abstract public void React();
    abstract public void React2();
}
