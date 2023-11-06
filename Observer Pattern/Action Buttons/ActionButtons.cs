using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using ObserverPattern;

public class ActionButtons : Subject // 참고: ObserverPattern의 Subject를 상속받았으므로 옵저버의 관찰 대상이 될 객체의 클래스다.
{
    private Player player;
    private readonly Dictionary<int, UISprite> buttonIcons = new Dictionary<int, UISprite>();
    private int buttons;

    private void Awake()
    {
        int i;
        var buttonObservers = GetComponentsInChildren<IObserver>(true);
        buttons = buttonObservers.Length;
        for (i = 0; i < buttons; ++i)
        {
            AddObserver(buttonObservers[i]); // 옵저버 추가하기
        }

        i = 0;
        buttonIcons.Add(i++, null);
        var iconSprites = GetComponentsInChildren<UISprite>(true);
        foreach (var sprite in iconSprites.Where(sprite => sprite.gameObject.name.Equals("Icon")))
        {
            buttonIcons.Add(i++, sprite);
        }
    }

    private void Start()
    {
        player = FindObjectOfType<Player>();
        player.onUpdateVisibleGlobalCoolTime = new UnityEvent();
        player.onUpdateVisibleGlobalCoolTime.AddListener(UpdateVisibleCoolTime); // 플레이어 UnityEvent에 리스너 달기

        player.onUpdateSqrDistanceFromCurrentTarget.AddListener(UpdateActionUsableness);
    }

    private void UpdateVisibleCoolTime()
    {
        Notify();
    }

    private void UpdateActionUsableness()
    {
        Notify2();
    }

    public UISprite GetActionIcon(int actionID)
    {
        return buttonIcons[actionID];
    }
}
