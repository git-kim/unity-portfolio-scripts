using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class BuffInformationList
{
    [SerializeField] private List<BuffInformationObject> buffInformationObjects;
    public BuffInformationObject this[int index]
    {
        get { return buffInformationObjects[index]; }
        set { buffInformationObjects[index] = value; }
    }

    public int Count => buffInformationObjects.Count;
}

public enum BuffType
{
    Buff,
    Debuff
}

[CreateAssetMenu(fileName = "Buff Information", menuName = "Scriptable Object/Buff Information", order = 2)]
public class BuffInformationObject : ScriptableObject
{
    public string buffName;
    public BuffType type;
    public float effectTime;
    public string spriteName;
    public string description;
    public GameObject indicatorPrefab;
}
