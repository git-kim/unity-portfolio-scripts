using System.Collections.Generic;
using System.Linq;
using Characters;
using Characters.EnemySkills;
using Characters.StatisticsScripts;
using Enums;
using Managers;
using UnityEditor;
using UnityEngine;

namespace VideoCapture
{
    /// <summary>
    /// Watches a play session from the editor and logs the moments worth a subtitle.
    /// Times are video seconds since Begin: recorded frames / 60, so they stay right while the
    /// autopilot fast-forwards the game with Time.timeScale.
    /// </summary>
    public class GameEventWatcher
    {
        private const float AoeHitThreshold = 500f; // the boss AOE deals ~1000, melee ~130
        private const float AoeResultDelay = 0.3f;

        private readonly List<GameEvent> events = new();
        private const float VideoFrameRate = 60f; // RecordOnPlay records at a constant 60 fps
        private int startFrame;

        /// <summary>The watcher of the take being recorded, so the autopilot can mark staged moments.</summary>
        public static GameEventWatcher Active { get; private set; }
        private GameManager gameManager;
        private Player player;
        private DoorController[] doors;
        private int skeletonCount;
        private int skeletonsDown;

        private bool battleSeen;
        private bool bossSeen;
        private bool doorSeen;
        private bool aoeActive;
        private float aoeHitPointsBefore;
        private float aoeResultTime = -1f;

        public IReadOnlyList<GameEvent> Events => events;
        public bool IsRunning { get; private set; }

        public void Begin()
        {
            events.Clear();
            startFrame = Time.frameCount;
            seenIds.Clear();
            LastSubtitleTime = float.NegativeInfinity;
            gameManager = GameManager.ExistingInstance;
            player = Object.FindAnyObjectByType<Player>();
            doors = Object.FindObjectsByType<DoorController>(FindObjectsInactive.Exclude);
            skeletonCount = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).Count(e => !e.IsBoss);
            skeletonsDown = 0;
            battleSeen = bossSeen = doorSeen = aoeActive = false;
            aoeResultTime = -1f;

            if (gameManager)
            {
                gameManager.EnemyDied += OnEnemyDied;
                gameManager.GameEnded += OnGameEnded;
            }
            EditorApplication.update += Poll;
            IsRunning = true;
            Active = this;
            Add("start");
        }

        public void End()
        {
            if (!IsRunning)
                return;
            IsRunning = false;
            if (Active == this)
                Active = null;
            EditorApplication.update -= Poll;
            if (gameManager)
            {
                gameManager.EnemyDied -= OnEnemyDied;
                gameManager.GameEnded -= OnGameEnded;
            }
        }

        /// <summary>Video seconds since Begin.</summary>
        public float Now => (Time.frameCount - startFrame) / VideoFrameRate;

        /// <summary>When the latest event that shows a subtitle happened, in video seconds.</summary>
        public float LastSubtitleTime { get; private set; } = float.NegativeInfinity;

        private readonly HashSet<string> seenIds = new();

        public void Add(string id, string detail = "")
        {
            events.Add(new GameEvent(Now, id, detail));
            if (Captions.Line(id, detail, seenIds.Add(id), korean: true) != null)
                LastSubtitleTime = Now;
        }

        private void OnEnemyDied(Enemy enemy)
        {
            if (enemy.IsBoss)
                Add("boss-down");
            else
                Add("skeleton-down", $"{++skeletonsDown}/{skeletonCount}");
        }

        private void OnGameEnded(GameState state) => Add(state == GameState.Cleared ? "cleared" : "over");

        private void Poll()
        {
            if (!EditorApplication.isPlaying || !gameManager)
            {
                End();
                return;
            }

            if (!battleSeen && gameManager.IsInBattle)
            {
                battleSeen = true;
                Add("first-battle");
            }
            if (!bossSeen && gameManager.IsBossEngaged)
            {
                bossSeen = true;
                Add("boss-engaged");
            }
            if (!doorSeen && doors.Length > 0 && doors.All(d => d && d.IsUnlocked))
            {
                doorSeen = true;
                Add("door-open");
            }

            var casting = Object.FindAnyObjectByType<AoeTelegraph>() != null;
            if (casting && !aoeActive)
            {
                Add("aoe-cast");
                aoeHitPointsBefore = HitPoints;
            }
            else if (!casting && aoeActive)
                aoeResultTime = Time.time + AoeResultDelay;
            aoeActive = casting;

            if (aoeResultTime >= 0f && Time.time >= aoeResultTime)
            {
                aoeResultTime = -1f;
                var hit = aoeHitPointsBefore - HitPoints >= AoeHitThreshold;
                if (hit || gameManager.State == GameState.Running) // a cast cut short by the boss dying is neither
                    Add(hit ? "aoe-hit" : "aoe-dodged");
            }
        }

        private float HitPoints => player ? player.Stats[Stat.HitPoints] : 0f;
    }
}
