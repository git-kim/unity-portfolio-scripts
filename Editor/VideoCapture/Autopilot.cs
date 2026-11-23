using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Characters;
using Characters.EnemySkills;
using Characters.Handlers;
using Characters.StatisticsScripts;
using Enums;
using Managers;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace VideoCapture
{
    /// <summary>
    /// Plays one run of the scene through simulated keyboard and mouse input (the same path a player's
    /// input takes), staging the features the portfolio highlights: camera zoom/orbit, walk/run toggle,
    /// battle pose, sprint, jump, click targeting with head look-at, a cast cancelled by moving, GCD and
    /// off-GCD skills, the target marker moving, foot IK on the stairs, the boss AOE dodge and the result.
    /// Editor-only; the game code is untouched.
    /// </summary>
    [InitializeOnLoad]
    public class Autopilot
    {
        private const string OnceKey = "VideoCapture.AutopilotOnce";
        private const string QuitKey = "VideoCapture.QuitAfterTake";
        private static Autopilot active;

        private readonly Stack<IEnumerator> script = new();
        private readonly HashSet<Key> keys = new();
        private Keyboard keyboard;
        private Mouse mouse;
        private Vector2 mousePosition, mouseDelta, scroll;
        private bool leftButton, rightButton;
        private InputSettings.BackgroundBehavior savedBackground;
        private InputSettings.EditorInputBehaviorInPlayMode savedEditorBehavior;
        private Action finished;

        private Player player;
        private PlayerActionHandler actions;
        private Enemy[] skeletons;
        private Enemy boss;
        private float healReadyAt, manaReadyAt;
        private Key heldMoveA, heldMoveB;
        private float yawPerDelta = 0.1f; // camera degrees per unit of mouse delta (signed), measured in Orbit
        private bool tracking;
        private bool wantFast;
        private const float SubtitleHold = 2.5f; // 1x long enough to read; the subtitle itself stays up 3.5 s
        private GameObject fastForwardBadge;

        public string Step { get; private set; } = "";
        public bool IsRunning { get; private set; }
        public readonly List<string> Log = new();

        static Autopilot() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        [MenuItem("Tools/Video/Record Autopilot Take")]
        private static void RecordTakeMenu() => RecordTake(false);

        /// <summary>Command-line entry: record one autopilot take, then quit the editor.</summary>
        public static void RecordTakeAndQuit() => RecordTake(true);

        private static void RecordTake(bool quit)
        {
            SessionState.SetBool(OnceKey, true);
            SessionState.SetBool(RecordOnPlay.OnceKey, true);
            SessionState.SetBool(QuitKey, quit);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Scene 1.unity");
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(OnceKey, false))
            {
                SessionState.SetBool(OnceKey, false);
                active = new Autopilot();
                active.Start(() => EditorApplication.delayCall += EditorApplication.ExitPlaymode);
            }
            else if (change == PlayModeStateChange.ExitingPlayMode)
            {
                active?.Stop();
                active = null;
            }
            else if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(QuitKey, false))
            {
                SessionState.SetBool(QuitKey, false);
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
        }

        public void Start(Action onFinished)
        {
            finished = onFinished;
            player = Object.FindAnyObjectByType<Player>();
            actions = player.GetComponentInChildren<PlayerActionHandler>();
            var enemies = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude);
            skeletons = enemies.Where(e => !e.IsBoss).OrderBy(e => e.transform.position.x).ToArray();
            boss = enemies.First(e => e.IsBoss);

            var settings = InputSystem.settings;
            savedBackground = settings.backgroundBehavior;
            savedEditorBehavior = settings.editorInputBehaviorInPlayMode;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>("Autopilot Keyboard");
            mouse = InputSystem.AddDevice<Mouse>("Autopilot Mouse");
            keyboard.MakeCurrent();
            mouse.MakeCurrent();
            mousePosition = new Vector2(Screen.width / 2f, Screen.height / 2f);

            script.Clear();
            script.Push(Run());
            InputSystem.onBeforeUpdate += Tick;
            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning)
                return;
            IsRunning = false;
            InputSystem.onBeforeUpdate -= Tick;
            SetFastForward(false);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = savedBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = savedEditorBehavior;
        }

        // Runs once per frame before the game reads input: advance the script, then send this frame's state.
        private void Tick()
        {
            if (InputState.currentUpdateType != InputUpdateType.Dynamic || !IsRunning)
                return;

            keys.Clear();
            mouseDelta = scroll = Vector2.zero;
            leftButton = false;
            if (heldMoveA != Key.None) keys.Add(heldMoveA);
            if (heldMoveB != Key.None) keys.Add(heldMoveB);

            try
            {
                while (script.Count > 0)
                {
                    var top = script.Peek();
                    if (!top.MoveNext())
                    {
                        script.Pop();
                        continue;
                    }
                    if (top.Current is IEnumerator sub)
                    {
                        script.Push(sub);
                        continue;
                    }
                    break;
                }
            }
            catch (Exception e)
            {
                Note("failed: " + e);
                script.Clear(); // end the run rather than leave the character stuck
            }

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
            var state = new MouseState { position = mousePosition, delta = mouseDelta, scroll = scroll }
                .WithButton(MouseButton.Left, leftButton)
                .WithButton(MouseButton.Right, rightButton);
            InputSystem.QueueStateEvent(mouse, state);

            // Fast-forward only while nothing is being explained on screen.
            var watcher = GameEventWatcher.Active;
            SetFastForward(wantFast && !(watcher != null && watcher.Now - watcher.LastSubtitleTime < SubtitleHold));

            if (script.Count == 0)
            {
                Note("done");
                Stop();
                finished?.Invoke();
            }
        }

        private void Note(string step)
        {
            Step = step;
            Log.Add($"{Time.time:0.00}\t{step}\tHP {player.Stats[Stat.HitPoints]} MP {player.Stats[Stat.ManaPoints]}");
        }

        // ---- the run -------------------------------------------------------------------------------

        private IEnumerator Run()
        {
            Note("intro");
            yield return Wait(3.6f); // let the title subtitle run its full length

            Mark("camera");
            for (var i = 0; i < 5; i++) { scroll = Vector2.down; yield return Wait(0.25f); }
            yield return Wait(0.6f);
            for (var i = 0; i < 5; i++) { scroll = Vector2.up; yield return Wait(0.25f); }

            Note("orbit");
            yield return Orbit(360f, 2f);

            Mark("walk");
            yield return Tap(Key.Slash);
            yield return Hold(1.6f, Key.W);
            yield return Tap(Key.Slash);

            yield return Wait(0.4f);
            Mark("battle-pose");
            yield return Tap(Key.Z);
            yield return Wait(1f);
            yield return Tap(Key.Z);
            yield return Wait(0.6f);

            Mark("sprint");
            yield return Tap(Key.Q);
            yield return Wait(1.5f); // the buff shows first; jumping in the same moment looked odd
            Note("run + jump");
            yield return MoveTo(new Vector3(0f, 0f, -33f), 0.6f, jumpAtZ: (player.transform.position.z - 33f) / 2f);
            yield return Wait(0.6f);

            Mark("head-ik");
            yield return Click(skeletons[0]);
            yield return Wait(0.6f);
            yield return Hold(0.8f, Key.D);
            yield return Wait(0.3f);
            yield return Hold(0.8f, Key.A);
            yield return Wait(0.5f);

            Mark("cast-cancel");
            yield return Tap(Key.Digit1);
            yield return Wait(0.35f);
            yield return Hold(0.25f, Key.S);
            yield return Wait(0.8f);

            Note("fight skeleton 1");
            yield return Fight(skeletons[0]);
            Mark("target-switch");
            yield return Fight(skeletons[1]);
            yield return Wait(1.5f);

            Note("stairs"); // the left flight, where the camera sees the steps
            wantFast = true; // a walk with nothing new to say
            yield return MoveTo(new Vector3(-1.5f, 0f, -16.8f), 0.8f);
            yield return MoveTo(new Vector3(-10f, 0f, -17f), 0.8f);
            yield return MoveTo(new Vector3(-10f, 0f, -10.5f), 0.8f);
            yield return MoveTo(new Vector3(-1f, 0f, -10.5f), 0.8f);
            yield return AlignCamera(Vector3.forward, 2f);
            yield return MoveTo(new Vector3(0f, 0f, -5f), 0.8f);

            Note("boss room");
            yield return MoveTo(new Vector3(0f, 0f, 4f), 0.8f);
            // Foot IK: stand across the slope of a large gold pile (about 11.5 m from the boss, outside its
            // 10 m notice) so the feet rest at different heights, and look at the player from the front.
            var spot = SlopeSpot();
            yield return MoveTo(spot + Vector3.back * 4f, 0.4f);
            yield return MoveTo(spot, 0.25f); // walking +z ends facing along the slope's contour
            wantFast = false;
            Note($"standing on the pile at height {player.transform.position.y:0.00}");
            yield return Wait(0.4f);
            yield return AlignCamera(Vector3.back, 2.5f);
            // The composition keeps the character low in frame, so zooming in hides the feet under the HUD:
            // raise the character on screen for this shot only.
            yield return Reframe(-0.22f, 0.6f);
            Mark("feet-ik");
            for (var i = 0; i < 6; i++) { scroll = Vector2.up; yield return Wait(0.2f); }
            yield return Wait(2.5f);
            for (var i = 0; i < 6; i++) { scroll = Vector2.down; yield return Wait(0.15f); }
            yield return Reframe(0.22f, 0.6f);
            // The IK shot leaves the camera facing away from the boss; turn first, or it walks in through the camera.
            yield return AlignCamera(Flat(boss.transform.position - player.transform.position).normalized, 2f);
            Note("fight boss"); // the boss is within fireball range of the pile
            yield return Fight(boss);

            Note("result");
            yield return Wait(2.7f);
        }

        private IEnumerator Fight(Enemy enemy)
        {
            var cursed = false;
            while (enemy && !enemy.IsDead && !player.IsDead && GameManager.Instance.State == GameState.Running)
            {
                TrackCamera(enemy.transform.position);
                // Fights run at 2x whenever no subtitle is up (Tick checks that), and the boss's last
                // moments play at 1x so its fall is seen.
                wantFast = !(enemy == boss && enemy.Stats[Stat.HitPoints] < enemy.Stats[Stat.MaximumHitPoints] * 0.08f);
                var telegraph = Object.FindAnyObjectByType<AoeTelegraph>();
                if (telegraph)
                {
                    yield return Dodge(telegraph);
                    continue;
                }
                if (actions.CurrentTarget != enemy.gameObject)
                {
                    yield return Click(enemy);
                    yield return Wait(0.2f);
                    if (actions.CurrentTarget != enemy.gameObject) // the click missed (wall, floor edge): step closer and retry
                    {
                        Note("click missed " + enemy.name);
                        for (var end = Time.time + 0.6f; Time.time < end;)
                        {
                            Steer(enemy.transform.position);
                            yield return null;
                        }
                        Release();
                    }
                    continue;
                }
                if (Flat(enemy.transform.position - player.transform.position).magnitude > 22f)
                {
                    Steer(enemy.transform.position);
                    yield return null;
                    continue;
                }
                Release();

                var hp = player.Stats[Stat.HitPoints];
                var mp = player.Stats[Stat.ManaPoints];
                // Off-GCD shows best right after an instant skill: the global cooldown is still running, and
                // the game takes an off-GCD skill then, but not during a cast (a key pressed mid-cast is dropped).
                if (!actions.IsCasting && actions.ActionBeingTaken == 0 && actions.InvisibleGlobalCoolDownTime == 0f
                    && actions.VisibleGlobalCoolDownTime > 0.3f && mp < player.Stats[Stat.MaximumManaPoints] * 0.7f // after a heal, not the opening curse
                    && Time.time >= manaReadyAt)
                {
                    manaReadyAt = Time.time + 61f;
                    yield return UseSelfBuff(Key.Digit4, ManaRestoreBuff, "off-gcd");
                    continue;
                }
                if (actions.IsCasting || actions.VisibleGlobalCoolDownTime > 0.05f || actions.InvisibleGlobalCoolDownTime > 0f)
                {
                    yield return null;
                    continue;
                }
                if (hp < player.Stats[Stat.MaximumHitPoints] * 0.9f && mp >= 700 && Time.time >= healReadyAt)
                {
                    healReadyAt = Time.time + 10f;
                    yield return UseSelfBuff(Key.Digit3, HealBuff, "heal");
                }
                else if (!cursed && mp >= 500)
                {
                    cursed = true;
                    Note("curse");
                    yield return Tap(Key.Digit2);
                }
                else if (mp >= 250)
                    yield return Tap(Key.Digit1);
                yield return Wait(0.2f);
            }
            Release();
            StopTracking();
            wantFast = false;
        }

        private const int HealBuff = 1, ManaRestoreBuff = 2; // buff indices PlayerActionHandler gives these skills

        // Press a self-buff key and caption it only once the buff is really on.
        private IEnumerator UseSelfBuff(Key key, int buffIndex, string id)
        {
            var buffs = actions.GetComponent<StatChangeHandler>();
            yield return Tap(key);
            for (var end = Time.time + 0.5f; Time.time < end; )
            {
                if (buffs.HasStatChangingEffect(buffIndex))
                {
                    Mark(id);
                    yield break;
                }
                yield return null;
            }
            Note(id + " did not take effect");
        }

        private IEnumerator Dodge(AoeTelegraph telegraph)
        {
            var radius = boss.GetComponent<AoeCastSkill>().Radius;
            var center = telegraph.transform.position;
            var away = Flat(player.transform.position - center);
            if (away.magnitude > radius + 1f)
            {
                Release();
                yield break;
            }
            Note("dodge AOE");
            if (away.sqrMagnitude < 0.01f)
                away = Flat(Camera.main.transform.right);
            // Leave the circle by the shortest way that stays inside the boss room: backing out through
            // the doorway would close the doors behind the player.
            var goal = Enumerable.Range(0, 8)
                .Select(i => center + Quaternion.Euler(0f, i * 45f, 0f) * away.normalized * (radius + 3f))
                .Where(g => Mathf.Abs(g.x) < 20f && g.z > 4f && g.z < 44f)
                .OrderBy(g => (g - player.transform.position).sqrMagnitude)
                .DefaultIfEmpty(center + away.normalized * (radius + 3f))
                .First();
            yield return MoveTo(goal, 0.6f, timeout: 4f, dodge: false);
            while (telegraph)
                yield return null;
        }

        // ---- input helpers -------------------------------------------------------------------------

        private static IEnumerator Wait(float seconds)
        {
            for (var end = Time.time + seconds; Time.time < end;)
                yield return null;
        }

        private IEnumerator Tap(Key key)
        {
            keys.Add(key);
            yield return null;
        }

        private IEnumerator Hold(float seconds, Key key)
        {
            for (var end = Time.time + seconds; Time.time < end;)
            {
                heldMoveA = key;
                heldMoveB = Key.None;
                keys.Add(key);
                yield return null;
            }
            Release();
        }

        private IEnumerator Click(Enemy enemy)
        {
            var bounds = enemy.GetComponent<Collider>().bounds;
            mousePosition = Camera.main.WorldToScreenPoint(bounds.center);
            leftButton = true;
            yield return null;
        }

        private IEnumerator Orbit(float degrees, float seconds)
        {
            rightButton = true;
            yield return null;
            var turned = 0f;
            var signedTurn = 0f;
            var deltaSum = 0f;
            var yaw = Camera.main.transform.eulerAngles.y;
            var perFrame = degrees / (seconds * 60f);
            var delta = 2f;
            for (var end = Time.time + seconds * 3f; turned < degrees && Time.time < end;)
            {
                mouseDelta = new Vector2(delta, 0f);
                yield return null;
                var now = Camera.main.transform.eulerAngles.y;
                var signedStep = Mathf.DeltaAngle(yaw, now);
                var step = Mathf.Abs(signedStep);
                turned += step;
                signedTurn += signedStep;
                deltaSum += delta;
                yaw = now;
                if (step > 0.01f) // degrees per unit of mouse delta, measured as we go
                    delta = Mathf.Clamp(Mathf.Lerp(delta, delta * perFrame / step, 0.3f), 0.05f, 50f);
            }
            rightButton = false;
            if (deltaSum > 0f && Mathf.Abs(signedTurn) > 1f)
                yawPerDelta = signedTurn / deltaSum;
            yield return null;
        }

        // Keeps the fight in frame: right-drag the camera until it looks from the player toward the enemy.
        private void TrackCamera(Vector3 focus)
        {
            var toFocus = Flat(focus - player.transform.position);
            if (toFocus.sqrMagnitude < 1f)
                return;
            var error = Mathf.DeltaAngle(Camera.main.transform.eulerAngles.y, Quaternion.LookRotation(toFocus).eulerAngles.y);
            if (!tracking && Mathf.Abs(error) > 25f)
                tracking = true;
            else if (tracking && Mathf.Abs(error) < 5f)
                tracking = false;
            rightButton = tracking;
            if (tracking)
                mouseDelta = new Vector2(Mathf.Clamp(error * 0.06f, -2.5f, 2.5f) / yawPerDelta, 0f);
        }

        private void StopTracking() => tracking = rightButton = false;

        private IEnumerator AlignCamera(Vector3 direction, float timeout)
        {
            for (var end = Time.time + timeout; Time.time < end;)
            {
                TrackCamera(player.transform.position + direction * 10f);
                if (!tracking)
                    break;
                yield return null;
            }
            StopTracking();
            yield return null;
        }

        // The small piles use convex colliders that sit above the coins, which leaves the feet floating; the
        // large pile collides with its own mesh. Probe its west slope for the steepest point at mid height.
        private static Vector3 SlopeSpot()
        {
            var pile = GameObject.Find("SM_Prop_Gold_Pile_Large_01 (9)");
            var top = pile ? pile.transform.position : new Vector3(8.5f, 0f, 19.4f);
            var best = top + Vector3.left * 4f;
            var bestSlope = 0f;
            float? previous = null;
            for (var x = 0f; x < 6f; x += 0.1f)
            {
                var probe = top + Vector3.left * x;
                if (!Physics.Raycast(probe + Vector3.up * 5f, Vector3.down, out var hit, 10f, 1 << 9))
                    continue;
                if (previous is { } h && hit.point.y > 0.25f && hit.point.y < 0.8f && h - hit.point.y > bestSlope)
                {
                    bestSlope = h - hit.point.y;
                    best = new Vector3(probe.x, 0f, probe.z);
                }
                previous = hit.point.y;
            }
            return best;
        }

        // Video-only framing: shift where the camera composes the character (negative = higher on screen).
        private static IEnumerator Reframe(float screenY, float seconds)
        {
            var composer = Object.FindAnyObjectByType<Unity.Cinemachine.CinemachineRotationComposer>();
            if (!composer)
                yield break;
            var from = composer.Composition.ScreenPosition.y;
            for (var t = 0f; t < 1f;)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / seconds);
                var composition = composer.Composition;
                composition.ScreenPosition.y = Mathf.Lerp(from, from + screenY, Mathf.SmoothStep(0f, 1f, t));
                composer.Composition = composition;
                yield return null;
            }
        }

        private void Mark(string id)
        {
            Note(id);
            GameEventWatcher.Active?.Add(id);
        }

        private void SetFastForward(bool on)
        {
            if (on == (fastForwardBadge != null))
                return;
            Time.timeScale = on ? 2f : 1f;
            if (!on)
            {
                Object.Destroy(fastForwardBadge);
                fastForwardBadge = null;
                Note("1x");
                return;
            }
            Note("2x");
            fastForwardBadge = new GameObject("Autopilot 2x Badge", typeof(Canvas));
            var canvas = fastForwardBadge.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var label = new GameObject("Label", typeof(UnityEngine.UI.Text)).GetComponent<UnityEngine.UI.Text>();
            label.transform.SetParent(fastForwardBadge.transform, false);
            label.font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI Symbol", "Segoe UI", "Arial" }, 64);
            label.text = "\u25B6\u25B6 2\u00D7";
            label.fontSize = Mathf.RoundToInt(Screen.height * 0.05f);
            label.alignment = TextAnchor.UpperRight;
            label.color = new Color(1f, 1f, 1f, 0.9f);
            label.gameObject.AddComponent<UnityEngine.UI.Shadow>().effectDistance = new Vector2(2f, -2f);
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-Screen.width * 0.03f, -Screen.height * 0.04f);
            rect.sizeDelta = new Vector2(Screen.width * 0.3f, Screen.height * 0.1f);
        }

        private IEnumerator MoveTo(Vector3 goal, float tolerance, float jumpAtZ = float.NaN, float timeout = 20f, bool dodge = true)
        {
            var jumped = float.IsNaN(jumpAtZ);
            for (var end = Time.time + timeout; Time.time < end;)
            {
                if (Flat(goal - player.transform.position).magnitude <= tolerance)
                    break;
                if (dodge && Object.FindAnyObjectByType<AoeTelegraph>() is { } telegraph)
                    yield return Dodge(telegraph);
                Steer(goal);
                if (!jumped && player.transform.position.z >= jumpAtZ)
                {
                    jumped = true;
                    keys.Add(Key.Space);
                }
                yield return null;
            }
            if (Flat(goal - player.transform.position).magnitude > tolerance)
                Note($"MoveTo {goal} stopped short at {player.transform.position}");
            Release();
        }

        // Movement is camera-relative: pick the WASD combination whose direction is closest to the goal.
        private void Steer(Vector3 goal)
        {
            var dir = Flat(goal - player.transform.position);
            var cam = Camera.main.transform;
            var forward = Flat(cam.forward).normalized;
            var right = Flat(cam.right).normalized;
            var angle = Mathf.Atan2(Vector3.Dot(dir, right), Vector3.Dot(dir, forward)) * Mathf.Rad2Deg;
            var sector = Mathf.RoundToInt(angle / 45f);
            heldMoveA = heldMoveB = Key.None;
            switch ((sector + 8) % 8)
            {
                case 0: heldMoveA = Key.W; break;
                case 1: heldMoveA = Key.W; heldMoveB = Key.D; break;
                case 2: heldMoveA = Key.D; break;
                case 3: heldMoveA = Key.S; heldMoveB = Key.D; break;
                case 4: heldMoveA = Key.S; break;
                case 5: heldMoveA = Key.S; heldMoveB = Key.A; break;
                case 6: heldMoveA = Key.A; break;
                case 7: heldMoveA = Key.W; heldMoveB = Key.A; break;
            }
            keys.Add(heldMoveA);
            if (heldMoveB != Key.None) keys.Add(heldMoveB);
        }

        private void Release() => heldMoveA = heldMoveB = Key.None;

        private static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);
    }
}
