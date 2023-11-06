using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum GameState { Running, CutscenePlaying, Over }

public class GameManager : Singleton<GameManager>
{
    private readonly object lockObj = new object();
    public Dictionary<int, Transform> PlayersAlive { get; set; } = new Dictionary<int, Transform>();
    public Dictionary<int, Transform> EnemiesAlive { get; set; } = new Dictionary<int, Transform>();

    [NonSerialized] public bool IsInBattle = false;

    public int AddPlayerAlive(int id, Transform playerTransform)
    {
        lock(lockObj)
        {
            while (PlayersAlive.ContainsKey(id))
            {
                ++id;
            }

            PlayersAlive.Add(id, playerTransform);
            return id;
        }
    }

    public int AddEnemyAlive(int id, Transform enemyTransform)
    {
        lock (lockObj)
        {
            while (EnemiesAlive.ContainsKey(id))
            {
                ++id;
            }

            EnemiesAlive.Add(id, enemyTransform);
            return id;
        }
    }

    private ErrorMessageDisplay errorMessageDisplay;

    private GameManager() { } // '비싱글턴(non-singleton) 생성자 사용' 방지

    public GameState State { get; set; } = GameState.Running;

    public float MasterVolume { get; set; } = 0.5f;
    public float BGMVolume { get; set; } = 1f;
    public float SFXVolume { get; set; } = 1f;

    [HideInInspector] public UnityEvent onGameTick;
    [HideInInspector] public UnityEvent onGameTick2;

    [SerializeField]
    List<BuffInformationObject> buffs = new BuffInfoList(); // 주의: = new List<BuffInformationObject>();을 사용하면 이 변수의 형을 BuffInfoList으로 변환할 수 없다.
    public BuffInfoList Buffs => (BuffInfoList)buffs;

    public void Awake()
    {
        errorMessageDisplay = FindObjectOfType<ErrorMessageDisplay>();
    }

    public void Start()
    {
        InvokeRepeating(nameof(Tick), 0.5f, 2.5f);
        InvokeRepeating(nameof(Tick2), 1.0f, 1.5f);
    }

    public bool CheckCylinder(Vector3 start, Vector3 end, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        Debug.DrawLine(start, end, Color.red);

        if (!Physics.CheckCapsule(start, end, radius, layerMask, queryTriggerInteraction))
            return false;

        var startToEnd = end - start;
        return Physics.CheckBox(start + 0.5f * startToEnd,
            new Vector3(radius, radius, startToEnd.magnitude * 0.5f),
            Quaternion.LookRotation(startToEnd), layerMask, queryTriggerInteraction);
    }

    public void ShowErrorMessage(int errMsgID)
    {
        errorMessageDisplay.ShowErrorMessage(errMsgID);
    }

    void Tick()
    {
        onGameTick.Invoke();
        
        if (IsInBattle && EnemiesAlive.Count == 0)
            IsInBattle = false;
        else if (IsInBattle && PlayersAlive.Count == 0)
        {
            IsInBattle = false;
            State = GameState.Over;
        }
    }

    void Tick2()
    {
        onGameTick2.Invoke();
    }
}