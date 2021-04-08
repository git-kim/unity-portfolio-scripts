using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBuffInfoDisplay : MonoBehaviour
{
    class BuffInfoObj
    {
        public int id;
        public GameObject prefab;
        public float time;
        public UILabel timeLabel;

        public BuffInfoObj(int id, GameObject prefab, UILabel timeLabel)
        {
            this.id = id;
            this.prefab = prefab;
            this.timeLabel = timeLabel;
        }
    }

    Player player;
    BuffInfoList buffs;
    readonly List<BuffInfoObj> currentActiveBuffs = new List<BuffInfoObj>();
    readonly List<BuffInfoObj> availableBuffs = new List<BuffInfoObj>();
    UIGrid displayGrid;
    int maxDisplayableBuffs; // 리마인더: 버프 수가 많지 않아서 사용하지 않았다.

    void Awake()
    {
        displayGrid = gameObject.GetComponent<UIGrid>();
        maxDisplayableBuffs = displayGrid.maxPerLine;
    }

    void Start()
    {
        player = FindObjectOfType<Player>();
        buffs = GameManager.Instance.Buffs;

        foreach (BuffInformationObject buff in buffs)
        {
            GameObject prefab = Instantiate(buff.indicatorPrefab);
            prefab.transform.SetParent(gameObject.transform, false);
            prefab.SetActive(false);

            availableBuffs.Add(new BuffInfoObj(buff.id, prefab, prefab.GetComponentInChildren<UILabel>()));
        }
    }

    void FixedUpdate()
    {
        int listSize = currentActiveBuffs.Count;

        if (listSize == 0) return;
        if (player.IsDead)
        {
            foreach (BuffInfoObj buffInfoObj in currentActiveBuffs)
            {
                buffInfoObj.prefab.SetActive(false);
            }
            currentActiveBuffs.Clear();

            return;
        }
            
        foreach (BuffInfoObj currentActiveBuff in currentActiveBuffs)
        {
            currentActiveBuff.time = Mathf.Max(currentActiveBuff.time - Time.deltaTime, 0f);
            currentActiveBuff.timeLabel.text = (currentActiveBuff.time > 1f) ? currentActiveBuff.time.ToString("0") : currentActiveBuff.time.ToString("0.0");
        }
    }

    public void AddBuffToDisplay(int buffID, float effectTime)
    {
        int alreadyActiveIndex = currentActiveBuffs.FindIndex(element => element.id == buffID);
        if (alreadyActiveIndex != -1)
        {
            currentActiveBuffs[alreadyActiveIndex].time = effectTime;
            currentActiveBuffs[alreadyActiveIndex].timeLabel.text = effectTime.ToString();
        }
        else
        {
            int notYetActiveIndex = availableBuffs.FindIndex(element => element.id == buffID);
            BuffInfoObj buffInfoObj = availableBuffs[notYetActiveIndex];
            buffInfoObj.time = effectTime;
            buffInfoObj.timeLabel.text = effectTime.ToString();
            buffInfoObj.prefab.SetActive(true);
            currentActiveBuffs.Add(buffInfoObj);
            displayGrid.Reposition();
        }
    }

    public void RemoveDisplayingBuff(int buffID)
    {
        foreach (BuffInfoObj buff in currentActiveBuffs.FindAll(element => element.id == buffID))
        {
            buff.prefab.SetActive(false);
            currentActiveBuffs.Remove(buff);
        }

        displayGrid.Reposition();
    }

    public void RemoveAllDisplayingBuffs()
    {
        int listSize = currentActiveBuffs.Count;

        for (int i = listSize - 1; i >= 0; --i)
        {
            currentActiveBuffs[i].prefab.SetActive(false);
            currentActiveBuffs.RemoveAt(i);
        }
    }
}
