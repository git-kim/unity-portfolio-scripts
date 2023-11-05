using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatChangeDisplay : MonoBehaviour, IStatChangeDisplay
{
    BuffInfoList buffs;
    PlayerBuffInfoDisplay playerBuffInfoDisplay;
    //Actions playerActionCommands;
    ObjectPoolManager OBJP;
    Vector3 hPChangeObjectSpawnLocalPos;
    Vector3 buffOnOffObjectSpawnLocalPos;
    readonly List<GameObject> buffStartEndObjsInUse = new List<GameObject>();
    readonly List<GameObject> hPChangeObjsInUse = new List<GameObject>();
    int listSize;
    const float speed = 50f;

    void Awake()
    {
        OBJP = ObjectPoolManager.Instance;
        hPChangeObjectSpawnLocalPos = new Vector3(0f, -20f, 0f);
        buffOnOffObjectSpawnLocalPos = new Vector3(50f, -35f, 0f);
    }

    void Start()
    {
        buffs = GameManager.Instance.Buffs;
        //playerActionCommands = FindObjectOfType<Player>().ActionCommands;
        playerBuffInfoDisplay = FindObjectOfType<PlayerBuffInfoDisplay>();
    }

    public void ShowBuffEnd(int buffID)
    {
        GameObject obj = OBJP.SpawnObjectFromPool("BuffOffPlayer", Vector3.zero, Quaternion.identity, false);
        obj.transform.SetParent(gameObject.transform, false);
        obj.transform.localPosition = buffOnOffObjectSpawnLocalPos;

        UISprite objSprite = obj.GetComponentInChildren<UISprite>(true);
        objSprite.spriteName = buffs[buffID].spriteName;
        objSprite.alpha = 1f;

        UILabel objLabel = obj.GetComponentInChildren<UILabel>(true);
        objLabel.alpha = 1f;
        objLabel.text = "- " + buffs[buffID].buffName;

        buffStartEndObjsInUse.Add(obj);

        obj.SetActive(true);

        playerBuffInfoDisplay.RemoveDisplayingBuff(buffID);
    }

    public void ShowBuffStart(int buffID, float effectTime)
    {
        GameObject obj = OBJP.SpawnObjectFromPool("BuffOnPlayer", Vector3.zero, Quaternion.identity, false);
        obj.transform.SetParent(gameObject.transform, false);
        obj.transform.localPosition = buffOnOffObjectSpawnLocalPos;

        UISprite objSprite = obj.GetComponentInChildren<UISprite>(true);
        objSprite.spriteName = buffs[buffID].spriteName;
        objSprite.alpha = 1f;

        UILabel objLabel = obj.GetComponentInChildren<UILabel>(true);
        objLabel.alpha = 1f;
        objLabel.text = "+ " + buffs[buffID].buffName;

        buffStartEndObjsInUse.Add(obj);

        obj.SetActive(true);

        playerBuffInfoDisplay.AddBuffToDisplay(buffID, effectTime);
    }

    public void ShowHPChange(int change, bool isDecrement, in string actionName)
    {
        GameObject obj;

        if (!isDecrement)
        {
            obj = OBJP.SpawnObjectFromPool("HPRestorationPlayer", Vector3.zero, Quaternion.identity, false);
        }
        else
        {
            obj = OBJP.SpawnObjectFromPool("DamagePlayer", Vector3.zero, Quaternion.identity, false);
        }
        obj.transform.SetParent(gameObject.transform, false);
        obj.transform.localPosition = hPChangeObjectSpawnLocalPos;

        UILabel[] objLabels = obj.GetComponentsInChildren<UILabel>(true);
        objLabels[0].alpha = 1f;
        if (!(actionName is null)) objLabels[0].text = actionName;
        else objLabels[0].text = "";

        objLabels[1].alpha = 1f;
        objLabels[1].text = change.ToString();

        hPChangeObjsInUse.Add(obj);



        obj.SetActive(true);
    }

    public void ShowHPChangeOverTime(int change, bool isDecrement = false)
    {
        // 필요하지 않아 빈 상태로 두었다.
    }

    public void RemoveAllDisplayingBuffs()
    {
        playerBuffInfoDisplay.RemoveAllDisplayingBuffs();
    }

    void Update()
    {
        UpdateList(buffStartEndObjsInUse);
        UpdateList(hPChangeObjsInUse);
    }

    void UpdateList(List<GameObject> list)
    {
        if (list.Count > 0)
        {
            list.RemoveAll(obj => obj.activeSelf == false);

            listSize = list.Count;
            if (listSize > 1)
            {
                for (int i = 1; i < listSize; ++i)
                {
                    float differenceY = list[i].transform.localPosition.y - list[i - 1].transform.localPosition.y;
                    if (differenceY > 20f) continue;
                    list[i - 1].transform.localPosition += Vector3.down * (20f - differenceY);
                }
            }

            foreach (GameObject obj in list)
            {
                obj.transform.localPosition += Vector3.down * speed * Time.unscaledDeltaTime;
            }
        }
    }
}
