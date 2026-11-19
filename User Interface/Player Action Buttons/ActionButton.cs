using UnityEngine;
using ObserverPattern;
using UnityEngine.Events;
using Characters.Handlers;
using System;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UserInterface;

public abstract class ActionButton : MonoBehaviour, IObserver, IPointerDownHandler
{
    protected static Color UsablenessColor = new Color(0x00 / 255f, 0x6D / 255f, 0xA4 / 255f);
    protected static Color UnusablenessColor = new Color(0xA4 / 255f, 0x00 / 255f, 0x03 / 255f);

    public int actionID;
    public Image icon;
    [SerializeField] private TMP_Text manaPointsCostLabel;
    [SerializeField] private Key key = Key.None;
    [SerializeField] private UiFade clickEffect;

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

    protected virtual void Update()
    {
        if (key != Key.None && Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame)
            Press();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            Press();
    }

    private void Press()
    {
        if (clickEffect)
            clickEffect.Play();
        onPressed?.Invoke();
    }

    public abstract void React();
    public abstract void React2();
}
