using Characters;
using Managers;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace UserInterface
{
    public class EnemyStatChangeDisplay : MonoBehaviour, IStatChangeDisplay
    {
        private ObjectPoolManager objectPoolManagerInstance;

        private BuffInformationList buffs;

        private readonly List<GameObject> buffEndObjInUse = new List<GameObject>();
        private readonly List<GameObject> buffStartObjInUse = new List<GameObject>();
        private readonly List<GameObject> hPDownObjInUse = new List<GameObject>();
        private readonly List<GameObject> hPDownOverTimeObjInUse = new List<GameObject>();
        private int listSize;
        private Transform enemyTransform;
        private RectTransform thisTransform;
        private RectTransform parentTransform;
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
            thisTransform = (RectTransform)transform;
            parentTransform = (RectTransform)thisTransform.parent;

            hPChangeObjectSpawnLocalPosition = new Vector3(0f, 35f, 0f);
            hPChangeOverTimeObjectSpawnLocalPosition = new Vector3(0f, 20f, 0f);
            buffOnObjectSpawnLocalPosition = new Vector3(0f, 50f, 0f);
            buffOffObjectSpawnLocalPosition = new Vector3(0f, 50f, 0f);
        }

        public void Initialize(Enemy enemy, CharacterController characterController)
        {
            enemyObject = enemy.gameObject;
            enemyTransform = enemy.transform;
            offsetVector = enemyTransform.lossyScale.y * characterController.height * 0.5f * Vector3.up;
        }

        private void Start()
        {
            buffs = GameManager.Instance.Buffs;
        }

        private GameObject Spawn(string poolName, Vector3 localPosition)
        {
            var obj = objectPoolManagerInstance.SpawnObjectFromPool(poolName, Vector3.zero, Quaternion.identity, false);
            obj.transform.SetParent(thisTransform, false);
            obj.transform.localScale = Vector3.one;
            obj.transform.localPosition = localPosition;
            return obj;
        }

        public void ShowBuffEnd(int buffIndex)
        {
            var obj = Spawn("BuffOffEnemy", buffOffObjectSpawnLocalPosition);
            obj.GetComponentInChildren<Image>(true).sprite = buffs[buffIndex].icon;
            obj.GetComponentInChildren<TMP_Text>(true).text = "- " + buffs[buffIndex].buffName;
            buffEndObjInUse.Add(obj);
            obj.SetActive(true);
        }

        public void ShowBuffStart(int buffIndex, float effectTime)
        {
            var obj = Spawn("DebuffOnEnemy", buffOnObjectSpawnLocalPosition);
            // 참고: BuffOnEnemy라는 prefab은 만들지 않았다.
            obj.GetComponentInChildren<Image>(true).sprite = buffs[buffIndex].icon;
            obj.GetComponentInChildren<TMP_Text>(true).text = "+ " + buffs[buffIndex].buffName;
            buffStartObjInUse.Add(obj);
            obj.SetActive(true);
        }

        public void ShowHitPointsChange(int change, bool isDecrement, in string actionName)
        {
            if (!isDecrement) return;
            // 참고: 회복 prefab은 만들지 않았다.

            var obj = Spawn("DamageEnemy", hPChangeObjectSpawnLocalPosition);
            var objLabels = obj.GetComponentsInChildren<TMP_Text>(true);
            objLabels[0].text = actionName ?? "";
            objLabels[1].text = change.ToString();
            hPDownObjInUse.Add(obj);
            obj.SetActive(true);
        }

        public void ShowHitPointsChangeOverTime(int change, bool isDecrement = false)
        {
            if (!isDecrement) return;
            // 참고: 회복 prefab은 만들지 않았다.

            var obj = Spawn("DamageOverTimeEnemy", hPChangeOverTimeObjectSpawnLocalPosition);
            obj.GetComponentInChildren<TMP_Text>(true).text = change.ToString();
            hPDownOverTimeObjInUse.Add(obj);
            obj.SetActive(true);
        }

        private void LateUpdate()
        {
            if (!enemyObject || !enemyObject.activeSelf) return;

            UpdateList(buffEndObjInUse, false);
            UpdateList(buffStartObjInUse, true);
            UpdateList(hPDownObjInUse, true);
            UpdateList(hPDownOverTimeObjInUse, false);
        }

        // Follow the enemy only after Cinemachine has moved the camera this frame, or the text lags while it turns.
        private void OnEnable() => CinemachineCore.CameraUpdatedEvent.AddListener(FollowEnemyOnScreen);

        private void OnDisable() => CinemachineCore.CameraUpdatedEvent.RemoveListener(FollowEnemyOnScreen);

        private void FollowEnemyOnScreen(CinemachineBrain brain)
        {
            if (!enemyObject || !enemyObject.activeSelf) return;

            // The brain may update the camera before this script's Start runs, so use the camera it just moved.
            var screenPoint = brain.OutputCamera.WorldToScreenPoint(enemyTransform.position + offsetVector);
            if (screenPoint.z < 0f) // behind the camera
            {
                thisTransform.anchoredPosition = new Vector2(-100000f, -100000f);
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentTransform, screenPoint, null, out var localPoint);
            thisTransform.localPosition = localPoint;
        }

        public void RemoveAllDisplayingBuffs()
        {
            // not implemented yet.
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
}
