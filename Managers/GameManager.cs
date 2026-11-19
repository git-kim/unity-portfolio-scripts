using Characters;
using Enums;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UserInterface;

namespace Managers
{
    public class GameManager : Singleton<GameManager>
    {
        private GameManager() { }

        private readonly object lockObj = new object();
        public Dictionary<int, Transform> PlayersAlive { get; set; } = new Dictionary<int, Transform>();
        public Dictionary<int, Transform> EnemiesAlive { get; set; } = new Dictionary<int, Transform>();

        private readonly HashSet<Enemy> engagedEnemies = new HashSet<Enemy>();
        public bool IsInBattle => engagedEnemies.Count > 0;

        private ErrorMessageDisplay errorMessageDisplay;

        public GameState State { get; private set; } = GameState.Running;

        /// <summary>Raised once, when the boss dies (Cleared) or the player dies (Over).</summary>
        public event Action<GameState> GameEnded;

        /// <summary>Raised whenever any enemy dies.</summary>
        public event Action<Enemy> EnemyDied;

        public float MasterVolume { get; set; } = 0.5f;
        public float BGMVolume { get; set; } = 1f;
        public float SFXVolume { get; set; } = 1f;

        [NonSerialized] public UnityEvent onGameTick = new UnityEvent();
        [NonSerialized] public UnityEvent onGameTick2 = new UnityEvent();

        [SerializeField] private BuffInformationList buffs;
        public BuffInformationList Buffs => buffs;

        private Transform mainCameraTransform;
        public Transform MainCameraTransform
        {
            get
            {
                if (mainCameraTransform == null)
                    mainCameraTransform = Camera.main.SelfOrNull()?.transform;
                return mainCameraTransform;
            }
        }

        public void SetErrorMessageDisplay(ErrorMessageDisplay errorMessageDisplay)
        {
            this.errorMessageDisplay = errorMessageDisplay;
        }

        private void Awake()
        {
            UnityEngine.Random.InitState(Environment.TickCount);
        }

        public void Start()
        {
            InvokeRepeating(nameof(Tick), 0.5f, 2.5f);
            InvokeRepeating(nameof(Tick2), 1.0f, 1.5f);
        }

        public int AddPlayerAlive(int id, Transform playerTransform)
        {
            lock (lockObj)
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

        public void SetEngaged(Enemy enemy, bool isEngaged)
        {
            if (isEngaged)
                engagedEnemies.Add(enemy);
            else
                engagedEnemies.Remove(enemy);
        }

        public void NotifyEnemyDied(Enemy enemy)
        {
            engagedEnemies.Remove(enemy);
            EnemyDied?.Invoke(enemy);

            if (enemy.IsBoss)
                EndGame(GameState.Cleared);
        }

        /// <summary>Ends the game with the given result. Only the first call has an effect.</summary>
        public void EndGame(GameState result)
        {
            if (State != GameState.Running || result == GameState.Running)
                return;

            State = result;
            engagedEnemies.Clear();
            GameEnded?.Invoke(result);
        }

        public void ShowErrorMessage(int errMsgID)
        {
            errorMessageDisplay.ShowErrorMessage(errMsgID);
        }

        public void ShowMessage(string message)
        {
            errorMessageDisplay.SelfOrNull()?.ShowMessage(message);
        }

        void Tick()
        {
            onGameTick.Invoke();
        }

        void Tick2()
        {
            onGameTick2.Invoke();
        }
    }
}
