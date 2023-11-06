using System.Collections.Generic;
using UnityEngine;

public class EnemyStatChangeDisplay : MonoBehaviour, IStatChangeDisplay
{
    private BuffInfoList buffs;
    //Actions enemyActionCommands;
    private ObjectPoolManager objectPoolManagerInstance;

    private readonly List<GameObject> buffEndObjInUse = new List<GameObject>();
    private readonly List<GameObject> buffStartObjInUse = new List<GameObject>();
    private readonly List<GameObject> hPDownObjInUse = new List<GameObject>();
    private readonly List<GameObject> hPDownOverTimeObjInUse = new List<GameObject>();
    private int listSize;
    private Transform enemyTransform;
    private Transform thisTransform;
    private Camera mainCamera, nGUICamera;
    private GameObject enemyObject;
    private Vector3 offsetVector;
    private Vector3 hPChangeObjectSpawnLocalPosition;
    private Vector3 hPChangeOverTimeObjectSpawnLocalPosition;
    private Vector3 buffOnObjectSpawnLocalPosition;
    private Vector3 buffOffObjectSpawnLocalPosition;
    private const float Speed = 60f;

    private void Awake()
    {
        objectPoolManagerInstance = ObjectPoolManager.Instance;
        thisTransform = gameObject.transform;

        hPChangeObjectSpawnLocalPosition = new Vector3(0f, 35f, 0f);
        hPChangeOverTimeObjectSpawnLocalPosition = new Vector3(0f, 20f, 0f);
        buffOnObjectSpawnLocalPosition = new Vector3(0f, 50f, 0f);
        buffOffObjectSpawnLocalPosition = new Vector3(0f, 50f, 0f);
    }

    private void Start()
    {
        var enemy = FindObjectOfType<Enemy>();
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
        var obj = objectPoolManagerInstance.SpawnObjectFromPool("BuffOffEnemy", Vector3.zero, Quaternion.identity, false);
        obj.transform.SetParent(gameObject.transform, false);
        obj.transform.localScale = Vector3.one;
        obj.transform.localPosition = buffOffObjectSpawnLocalPosition;

        var objSprite = obj.GetComponentInChildren<UISprite>(true);
        objSprite.spriteName = buffs[buffID].spriteName;
        objSprite.alpha = 1f;

        var objLabel = obj.GetComponentInChildren<UILabel>(true);
        objLabel.alpha = 1f;
        objLabel.text = "- " + buffs[buffID].buffName;

        buffEndObjInUse.Add(obj);

        obj.SetActive(true);
    }

    public void ShowBuffStart(int buffID, float effectTime)
    {
        var obj = objectPoolManagerInstance.SpawnObjectFromPool("DebuffOnEnemy", Vector3.zero, Quaternion.identity, false);
        // 참고: BuffOnEnemy라는 prefab은 만들지 않았다.

        obj.transform.SetParent(gameObject.transform, false);
        obj.transform.localScale = Vector3.one;
        obj.transform.localPosition = buffOnObjectSpawnLocalPosition;

        var objSprite = obj.GetComponentInChildren<UISprite>(true);
        objSprite.spriteName = buffs[buffID].spriteName;
        objSprite.alpha = 1f;

        var objLabel = obj.GetComponentInChildren<UILabel>(true);
        objLabel.alpha = 1f;
        objLabel.text = "+ " + buffs[buffID].buffName;

        buffStartObjInUse.Add(obj);

        obj.SetActive(true);
    }

    public void ShowHPChange(int change, bool isDecrement, in string actionName)
    {
        if (!isDecrement) return;
        // 참고: 회복 prefab은 만들지 않았다.

        var obj = objectPoolManagerInstance.SpawnObjectFromPool("DamageEnemy", Vector3.zero, Quaternion.identity, false);
        obj.transform.SetParent(gameObject.transform, false);
        obj.transform.localScale = Vector3.one;
        obj.transform.localPosition = hPChangeObjectSpawnLocalPosition;

        var objLabels = obj.GetComponentsInChildren<UILabel>(true);
        objLabels[0].alpha = 1f;
        objLabels[0].text = actionName ?? "";
        objLabels[1].alpha = 1f;
        objLabels[1].text = change.ToString();

        hPDownObjInUse.Add(obj);

        obj.SetActive(true);
    }

    public void ShowHPChangeOverTime(int change, bool isDecrement = false)
    {
        if (!isDecrement) return;
        // 참고: 회복 prefab은 만들지 않았다.

        var obj = objectPoolManagerInstance.SpawnObjectFromPool("DamageOverTimeEnemy", Vector3.zero, Quaternion.identity, false);
        obj.transform.SetParent(gameObject.transform, false);
        obj.transform.localScale = Vector3.one;
        obj.transform.localPosition = hPChangeOverTimeObjectSpawnLocalPosition;

        var objLabel = obj.GetComponentInChildren<UILabel>(true);
        objLabel.alpha = 1f;
        objLabel.text = change.ToString();

        hPDownOverTimeObjInUse.Add(obj);

        obj.SetActive(true);
    }

    private void Update()
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

    private void UpdateList(List<GameObject> list, bool shouldMove)
    {
        if (list.Count <= 0)
            return;

        list.RemoveAll(obj => obj.activeSelf == false);

        if (!shouldMove)
            return;

        listSize = list.Count;
        if (listSize > 1)
        {
            for (var i = 1; i < listSize; ++i)
            {
                var differenceY = list[i - 1].transform.localPosition.y - list[i].transform.localPosition.y;
                if (differenceY > 20f) continue;
                list[i - 1].transform.localPosition += Vector3.up * (20f - differenceY);
            }
        }

        foreach (var obj in list)
        {
            obj.transform.localPosition += Vector3.up * (Speed * Time.unscaledDeltaTime);
        }
    }
}