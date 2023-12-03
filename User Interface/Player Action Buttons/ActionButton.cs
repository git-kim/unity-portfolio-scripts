using UnityEngine;
using ObserverPattern;
using UnityEngine.Events;
using Characters.Handlers;

public abstract class ActionButton : MonoBehaviour, IObserver
{
    public int actionID;
    public UISprite iconSprite;

    protected PlayerActionHandler playerActionHandler;
    private UnityAction onPressed;

    protected static Color UsablenessColor = new Color(0x00 / 255f, 0x6D / 255f, 0xA4 / 255f);
    protected static Color UnusablenessColor = new Color(0xA4 / 255f, 0x00 / 255f, 0x03 / 255f);

    public void Initialize(UnityAction onPressed, PlayerActionHandler playerActionHandler)
    {
        this.onPressed = onPressed;
        this.playerActionHandler = playerActionHandler;
    }

    public void OnPress(bool state)
    {
        if (state)
            onPressed?.Invoke();
    }

    public abstract void React();
    public abstract void React2();
}