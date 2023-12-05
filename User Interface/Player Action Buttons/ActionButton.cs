using UnityEngine;
using ObserverPattern;
using UnityEngine.Events;
using Characters.Handlers;
using System;

public abstract class ActionButton : MonoBehaviour, IObserver
{
    protected static Color UsablenessColor = new Color(0x00 / 255f, 0x6D / 255f, 0xA4 / 255f);
    protected static Color UnusablenessColor = new Color(0xA4 / 255f, 0x00 / 255f, 0x03 / 255f);

    public int actionID;
    public UISprite iconSprite;
    [SerializeField] private UILabel manaPointsCostLabel;

    [NonSerialized] protected PlayerActionHandler playerActionHandler;
    private UnityAction onPressed;

    [NonSerialized] protected int manaPointsCost = 0;
    [NonSerialized] protected float sqrRange = 0f;

    public virtual void Initialize(UnityAction onPressed,
        CharacterAction characterAction, PlayerActionHandler playerActionHandler)
    {
        this.onPressed = onPressed;
        this.playerActionHandler = playerActionHandler;
        if (manaPointsCostLabel)
            manaPointsCostLabel.text = characterAction.manaPointsCost.ToString();
        manaPointsCost = characterAction.manaPointsCost;
        sqrRange = Mathf.Pow(characterAction.range, 2f);
    }

    public void OnPress(bool state)
    {
        if (state)
            onPressed?.Invoke();
    }

    public abstract void React();
    public abstract void React2();
}