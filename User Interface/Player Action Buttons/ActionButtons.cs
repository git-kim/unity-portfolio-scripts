using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ObserverPattern;
using UnityEngine.UI;

public class ActionButtons : Subject, IEnumerable<ActionButton>
{
    private Dictionary<int, ActionButton> buttons;

    public ActionButton this[int actionID]
    {
        get { return buttons[actionID]; }
    }

    private void Awake()
    {
        var buttonList = GetComponentsInChildren<ActionButton>(true).ToList();
        var count = buttonList.Count;

        buttons = new(count + 1) { { 0, null } };

        for (var i = 0; i < count; ++i)
        {
            var button = buttonList[i];
            AddObserver(button);
            buttons.Add(button.actionID, button);
        }
    }

    public void UpdateVisibleCoolTime()
    {
        Notify();
    }

    public void UpdateActionUsableness()
    {
        Notify2();
    }

    public Image GetActionIcon(int actionID)
    {
        return buttons[actionID].icon;
    }

    IEnumerator<ActionButton> IEnumerable<ActionButton>.GetEnumerator()
    {
        return buttons.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return buttons.Values.GetEnumerator();
    }
}
