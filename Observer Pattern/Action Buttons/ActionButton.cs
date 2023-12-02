using UnityEngine;
using ObserverPattern;

public abstract class ActionButton : MonoBehaviour, IObserver
{
    public int actionID;

    protected static bool HasPlayerVariablesBeenSet = false;
    protected static Player Player;
    protected static CharacterActions PlayerActionCommands;
    protected static Color UsablenessColor = new Color(0x00 / 255f, 0x6D / 255f, 0xA4 / 255f);
    protected static Color UnusablenessColor = new Color(0xA4 / 255f, 0x00 / 255f, 0x03 / 255f);

    protected void SetPlayerVariables()
    {
        if (HasPlayerVariablesBeenSet)
            return;

        HasPlayerVariablesBeenSet = true;
        Player = FindObjectOfType<Player>();
        PlayerActionCommands = Player.CharacterActions;
    }

    // OnPress 트리거 발생 시 호출되는 함수
    public void OnPress(bool state)
    {
        if (state) // state가 true일 때
            Player.recentActionInput = actionID;
    }

    public abstract void React();
    public abstract void React2();
}