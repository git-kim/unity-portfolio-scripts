using System.Collections.Generic;
using UnityEngine;

public class PlayerStatChangeDisplay : MonoBehaviour, IStatChangeDisplay
{
    private BuffInformationList buffs;
    private PlayerBuffInfoDisplay playerBuffInfoDisplay;
    private ObjectPoolManager objectPoolManagerInstance;
    private Vector3 hPChangeObjectSpawnLocalPos;
    private Vector3 buffOnOffObjectSpawnLocalPos;
    private readonly List<GameObject> buffStartEndObjsInUse = new List<GameObject>();
    private readonly List<GameObject> hPChangeObjsInUse = new List<GameObject>();
    private int listSize;
    private const float Speed = 50f;

    private void Awake()
    {
        objectPoolManagerInstance = ObjectPoolManager.Instance;
        hPChangeObjectSpawnLocalPos = new Vector3(0f, -20f, 0f);
        buffOnOffObjectSpawnLocalPos = new Vector3(50f, -35f, 0f);
    }

    private void Start()
    {
        buffs = GameManager.Instance.Buffs;
        //playerActionCommands = FindObjectOfType<Player>().ActionCommands;
        playerBuffInfoDisplay = FindObjectOfType<PlayerBuffInfoDisplay>();
    }

    public void ShowBuffEnd(int buffIdentifier)
    {
        var obj = objectPoolManagerInstance.SpawnObjectFromPool("BuffOffPlayer", Vector3.zero, Quaternion.identity, false);
        obj.transform.SetParent(gameObject.transform, false);
        obj.transform.localPosition = buffOnOffObjectSpawnLocalPos;

        var objSprite = obj.GetComponentInChildren<UISprite>(true);
        objSprite.spriteName = buffs[buffIdentifier].spriteName;
        objSprite.alpha = 1f;

        var objLabel = obj.GetComponentInChildren<UILabel>(true);
        objLabel.alpha = 1f;
        objLabel.text = "- " + buffs[buffIdentifier].buffName;

        buffStartEndObjsInUse.Add(obj);

        obj.SetActive(true);

        playerBuffInfoDisplay.RemoveDisplayingBuff(buffIdentifier);
    }

    public void ShowBuffStart(int buffIdentifier, float effectTime)
    {
        var obj = objectPoolManagerInstance.SpawnObjectFromPool("BuffOnPlayer", Vector3.zero, Quaternion.identity, false);
        obj.transform.SetParent(gameObject.transform, false);
        obj.transform.localPosition = buffOnOffObjectSpawnLocalPos;

        var objSprite = obj.GetComponentInChildren<UISprite>(true);
        objSprite.spriteName = buffs[buffIdentifier].spriteName;
        objSprite.alpha = 1f;

        var objLabel = obj.GetComponentInChildren<UILabel>(true);
        objLabel.alpha = 1f;
        objLabel.text = "+ " + buffs[buffIdentifier].buffName;

        buffStartEndObjsInUse.Add(obj);

        obj.SetActive(true);

        playerBuffInfoDisplay.AddBuffToDisplay(buffIdentifier, effectTime);
    }

    public void ShowHitPointsChange(int change, bool isDecrement, in string actionName)
    {
        var obj = objectPoolManagerInstance.SpawnObjectFromPool(!isDecrement ?
                "HPRestorationPlayer" : "DamagePlayer",
            Vector3.zero, Quaternion.identity, false);
        obj.transform.SetParent(gameObject.transform, false);
        obj.transform.localPosition = hPChangeObjectSpawnLocalPos;

        var objLabels = obj.GetComponentsInChildren<UILabel>(true);
        objLabels[0].alpha = 1f;
        objLabels[0].text = actionName ?? "";
        objLabels[1].alpha = 1f;
        objLabels[1].text = change.ToString();

        hPChangeObjsInUse.Add(obj);

        obj.SetActive(true);
    }

    public void ShowHitPointsChangeOverTime(int change, bool isDecrement = false)
    {
        // 필요하지 않아 빈 상태로 두었다.
    }

    public void RemoveAllDisplayingBuffs()
    {
        playerBuffInfoDisplay.RemoveAllDisplayingBuffs();
    }

    private void Update()
    {
        UpdateList(buffStartEndObjsInUse);
        UpdateList(hPChangeObjsInUse);
    }

    private void UpdateList(List<GameObject> list)
    {
        if (list.Count <= 0)
            return;

        list.RemoveAll(obj => obj.activeSelf == false);

        listSize = list.Count;
        if (listSize > 1)
        {
            for (var i = 1; i < listSize; ++i)
            {
                var differenceY =
                    list[i].transform.localPosition.y - list[i - 1].transform.localPosition.y;
                if (differenceY > 20f) continue;
                list[i - 1].transform.localPosition += Vector3.down * (20f - differenceY);
            }
        }

        foreach (var obj in list)
        {
            obj.transform.localPosition += Vector3.down * (Speed * Time.unscaledDeltaTime);
        }
    }
}
