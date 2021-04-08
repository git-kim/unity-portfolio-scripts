using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStatChangeDisplay : MonoBehaviour, IStatChangeDisplay
{
    BuffInfoList buffs;
    //Actions enemyActionCommands;
    ObjectPoolManager OBJP;

    readonly List<GameObject> buffEndObjInUse = new List<GameObject>();
    readonly List<GameObject> buffStartObjInUse = new List<GameObject>();
    readonly List<GameObject> hPDownObjInUse = new List<GameObject>();
    readonly List<GameObject> hPDownOverTimeObjInUse = new List<GameObject>();
    int listSize;
    Transform enemyTransform;
    Transform thisTransform;
    Camera mainCamera, nGUICamera;
    GameObject enemyObject;
    Vector3 offsetVector;
    Vector3 hPChangeObjectSpawnLocalPos;
    Vector3 hPChangeOverTimeObjectSpawnLocalPos;
    Vector3 buffOnObjectSpawnLocalPos;
    Vector3 buffOffObjectSpawnLocalPos;
    const float speed = 60f;

    void Awake()
    {
        OBJP = ObjectPoolManager.Instance;
        thisTransform = gameObject.transform;

        hPChangeObjectSpawnLocalPos = new Vector3(0f, 35f, 0f);
        hPChangeOverTimeObjectSpawnLocalPos = new Vector3(0f, 20f, 0f);
        buffOnObjectSpawnLocalPos = new Vector3(0f, 50f, 0f);
        buffOffObjectSpawnLocalPos = new Vector3(0f, 50f, 0f);
    }

    void Start()
    {
        Enemy enemy = FindObjectOfType<Enemy>();
        enemyObject = enemy.gameObject;
        enemyTransform = enemy.transform;
        buffs = GameManager.Instance.Buffs;
        offsetVector = enemyTransform.lossyScale.y * enemy.GetComponent<CharacterController>().height * 0.5f * Vector3.up;
        //enemyActionCommands = enemy.ActionCommands;
        mainCamera = Camera.main;
        nGUICamera = GameObject.Find("UI Root").GetComponentInChildren<Camera>();
    }

    public void ShowBuffEnd(int buffID)
    {
        GameObject obj = OBJP.SpawnObjectFromPool("BuffOffEnemy", Vector3.zero, Quaternion.identity, false);
        obj.transform.SetParent(gameObject.transform, false);
        obj.transform.localScale = Vector3.one;
        obj.transform.localPosition = buffOffObjectSpawnLocalPos;

        UISprite objSprite = obj.GetComponentInChildren<UISprite>(true);
        objSprite.spriteName = buffs[buffID].spriteName;
        objSprite.alpha = 1f;

        UILabel objLabel = obj.GetComponentInChildren<UILabel>(true);
        objLabel.alpha = 1f;
        objLabel.text = "- " + buffs[buffID].buffName;

        buffEndObjInUse.Add(obj);

        obj.SetActive(true);
    }

    public void ShowBuffStart(int buffID, float effectTime)
    {
        GameObject obj = OBJP.SpawnObjectFromPool("DebuffOnEnemy", Vector3.zero, Quaternion.identity, false);
        // 참고: BuffOnEnemy라는 prefab은 만들지 않았다.

        obj.transform.SetParent(gameObject.transform, false);
        obj.transform.localScale = Vector3.one;
        obj.transform.localPosition = buffOnObjectSpawnLocalPos;

        UISprite objSprite = obj.GetComponentInChildren<UISprite>(true);
        objSprite.spriteName = buffs[buffID].spriteName;
        objSprite.alpha = 1f;

        UILabel objLabel = obj.GetComponentInChildren<UILabel>(true);
        objLabel.alpha = 1f;
        objLabel.text = "+ " + buffs[buffID].buffName;

        buffStartObjInUse.Add(obj);

        obj.SetActive(true);
    }

    public void ShowHPChange(int change, bool isDecrement, in string actionName)
    {
        if (!isDecrement) return;
        // 참고: 회복 prefab은 만들지 않았다.

        GameObject obj;

        obj = OBJP.SpawnObjectFromPool("DamageEnemy", Vector3.zero, Quaternion.identity, false);
        obj.transform.SetParent(gameObject.transform, false);
        obj.transform.localScale = Vector3.one;
        obj.transform.localPosition = hPChangeObjectSpawnLocalPos;

        UILabel[] objLabels = obj.GetComponentsInChildren<UILabel>(true);
        objLabels[0].alpha = 1f;
        if (!(actionName is null)) objLabels[0].text = actionName;
        else objLabels[0].text = "";

        objLabels[1].alpha = 1f;
        objLabels[1].text = change.ToString();

        hPDownObjInUse.Add(obj);

        obj.SetActive(true);
    }

    public void ShowHPChangeOverTime(int change, bool isDecrement = false)
    {
        if (!isDecrement) return;
        // 참고: 회복 prefab은 만들지 않았다.

        GameObject obj;

        obj = OBJP.SpawnObjectFromPool("DamageOverTimeEnemy", Vector3.zero, Quaternion.identity, false);
        obj.transform.SetParent(gameObject.transform, false);
        obj.transform.localScale = Vector3.one;
        obj.transform.localPosition = hPChangeOverTimeObjectSpawnLocalPos;

        UILabel objLabel = obj.GetComponentInChildren<UILabel>(true);
        objLabel.alpha = 1f;
        objLabel.text = change.ToString();

        hPDownOverTimeObjInUse.Add(obj);

        obj.SetActive(true);
    }

    void Update()
    {
        if (!enemyObject.activeSelf) return;

        UpdateList(buffEndObjInUse, false);
        UpdateList(buffStartObjInUse, true);
        UpdateList(hPDownObjInUse, true);
        UpdateList(hPDownOverTimeObjInUse, false);

        thisTransform.OverlayPosition(enemyTransform.position + offsetVector, mainCamera, nGUICamera);
    }

    public void RemoveAllDisplayingBuffs()
    {
        // 리마인더: 구현하지 않았다.
    }

    void UpdateList(List<GameObject> list, bool shouldMove)
    {
        if (list.Count > 0)
        {
            list.RemoveAll(obj => obj.activeSelf == false);


            if (shouldMove)
            {
                listSize = list.Count;
                if (listSize > 1)
                {
                    for (int i = 1; i < listSize; ++i)
                    {
                        float differenceY = list[i - 1].transform.localPosition.y - list[i].transform.localPosition.y;
                        if (differenceY > 20f) continue;
                        list[i - 1].transform.localPosition += Vector3.up * (20f - differenceY);
                    }
                }

                foreach (GameObject obj in list)
                {
                    obj.transform.localPosition += Vector3.up * speed * Time.unscaledDeltaTime;
                }
            }
        }
    }
}
