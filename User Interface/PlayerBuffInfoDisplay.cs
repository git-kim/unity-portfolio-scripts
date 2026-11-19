using Characters;
using Managers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UserInterface
{
    public class PlayerBuffInfoDisplay : MonoBehaviour
    {
        private class BuffInfoObj
        {
            public readonly int Index;
            public readonly GameObject Prefab;
            public float Time;
            public readonly TMP_Text TimeLabel;

            public BuffInfoObj(int index, GameObject prefab, TMP_Text timeLabel)
            {
                Index = index;
                Prefab = prefab;
                TimeLabel = timeLabel;
            }
        }

        [SerializeField] private float cellWidth = 15f; // was the NGUI UIGrid cell width

        private Player player;
        private BuffInformationList buffs;
        private readonly List<BuffInfoObj> currentActiveBuffs = new List<BuffInfoObj>();
        private readonly List<BuffInfoObj> availableBuffs = new List<BuffInfoObj>();

        private void Start()
        {
            player = FindAnyObjectByType<Player>();
            buffs = GameManager.Instance.Buffs;
            var count = buffs.Count;

            for (var i = 0; i < count; ++i)
            {
                var buff = buffs[i];

                var prefab = Instantiate(buff.indicatorPrefab, gameObject.transform, false);
                prefab.SetActive(false);

                availableBuffs.Add(new BuffInfoObj(i, prefab, prefab.GetComponentInChildren<TMP_Text>()));
            }
        }

        private void Update()
        {
            var listSize = currentActiveBuffs.Count;
            if (listSize == 0)
                return;

            if (player.IsDead)
            {
                foreach (var buffInfoObj in currentActiveBuffs)
                {
                    buffInfoObj.Prefab.SetActive(false);
                }

                currentActiveBuffs.Clear();

                return;
            }

            foreach (var currentActiveBuff in currentActiveBuffs)
            {
                currentActiveBuff.Time = Mathf.Max(currentActiveBuff.Time - Time.deltaTime, 0f);
                currentActiveBuff.TimeLabel.text = currentActiveBuff.Time > 1f ? currentActiveBuff.Time.ToString("0") : currentActiveBuff.Time.ToString("0.0");
            }
        }

        public void AddBuffToDisplay(int buffIndex, float effectTime)
        {
            var alreadyActiveIndex = currentActiveBuffs.FindIndex(element => element.Index == buffIndex);
            if (alreadyActiveIndex != -1)
            {
                currentActiveBuffs[alreadyActiveIndex].Time = effectTime;
                currentActiveBuffs[alreadyActiveIndex].TimeLabel.text = effectTime.ToString();
            }
            else
            {
                var notYetActiveIndex = availableBuffs.FindIndex(element => element.Index == buffIndex);
                var buffInfoObj = availableBuffs[notYetActiveIndex];
                buffInfoObj.Time = effectTime;
                buffInfoObj.TimeLabel.text = effectTime.ToString();
                buffInfoObj.Prefab.SetActive(true);
                currentActiveBuffs.Add(buffInfoObj);
                Reposition();
            }
        }

        public void RemoveDisplayingBuff(int buffIndex)
        {
            foreach (var buff in currentActiveBuffs.FindAll(element => element.Index == buffIndex))
            {
                buff.Prefab.SetActive(false);
                currentActiveBuffs.Remove(buff);
            }

            Reposition();
        }

        public void RemoveAllDisplayingBuffs()
        {
            var listSize = currentActiveBuffs.Count;

            for (var i = listSize - 1; i >= 0; --i)
            {
                currentActiveBuffs[i].Prefab.SetActive(false);
                currentActiveBuffs.RemoveAt(i);
            }
        }

        // Lays active buff icons out left to right, as NGUI's UIGrid.Reposition did.
        private void Reposition()
        {
            for (var i = 0; i < currentActiveBuffs.Count; ++i)
                currentActiveBuffs[i].Prefab.transform.localPosition = new Vector3(i * cellWidth, 0f, 0f);
        }
    }
}
