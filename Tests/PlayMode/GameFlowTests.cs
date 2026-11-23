using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Characters;
using Characters.EnemySkills;
using Characters.Handlers;
using Characters.StatisticsScripts;
using Enums;
using Managers;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

// Plays the real scene and checks the death, door-gate, boss-skill and result flows end to end.
public class GameFlowTests
{
    [UnitySetUp]
    public IEnumerator LoadScene()
    {
        SceneManager.LoadScene("Scene 1");
        yield return null;
        yield return null;
    }

    private static Enemy Boss => Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).Single(e => e.IsBoss);
    private static Enemy[] Skeletons => Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude)
        .Where(e => !e.IsBoss).OrderBy(e => e.name).ToArray();
    private static Player Player => Object.FindAnyObjectByType<Player>();

    private static void Kill(Component character) =>
        character.GetComponent<StatChangeHandler>().DecreaseStat(Stat.HitPoints, 1_000_000);

    private static void Teleport(Component character, Vector3 position)
    {
        if (character.TryGetComponent<NavMeshAgent>(out var agent) && agent.enabled)
        {
            agent.Warp(position);
            return;
        }
        var controller = character.GetComponent<CharacterController>();
        controller.enabled = false;
        character.transform.position = position;
        controller.enabled = true;
    }

    private static IEnumerator WaitSeconds(float seconds)
    {
        for (var end = Time.time + seconds; Time.time < end;)
            yield return null;
    }

    private static IEnumerator WaitUntil(Func<bool> condition, float timeout, string what)
    {
        var end = Time.time + timeout;
        while (!condition())
        {
            if (Time.time > end)
                Assert.Fail("Timed out waiting for " + what);
            yield return null;
        }
    }

    private static void SetPrivate(object target, Type declaringType, string field, object value) =>
        declaringType.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    private static void AssertDeathPose(Animator animator, string who)
    {
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Dead"), $"{who}: base layer is not in Dead");
        for (var layer = 1; layer < animator.layerCount; layer++)
            Assert.That(animator.GetCurrentAnimatorStateInfo(layer).IsName("No Override (Dead)"),
                $"{who}: layer {animator.GetLayerName(layer)} still overrides the death pose");
    }

    private static void AssertResultPanel(string title)
    {
        var panel = Object.FindAnyObjectByType<UserInterface.GameResultDisplay>().transform.Find("Panel");
        Assert.That(panel.gameObject.activeInHierarchy, "result panel is hidden");
        Assert.That(panel.Find("Title").GetComponent<TMP_Text>().text, Is.EqualTo(title));
    }

    [UnityTest]
    public IEnumerator EnemyHitPointsShowOnlyOnceTheFightStarts()
    {
        var skeleton = Skeletons[0];
        var bar = ((Component)typeof(Enemy).GetField("hitPointsDisplay", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(skeleton)).gameObject;
        Assert.That(bar.activeSelf, Is.False, "an idle enemy shows no bar");

        Teleport(Player, skeleton.transform.position + skeleton.transform.forward * 4f + Vector3.up * 0.2f);
        yield return WaitUntil(() => GameManager.Instance.IsInBattle, 5f, "the skeleton to engage");
        yield return null;
        Assert.That(bar.activeSelf, Is.True, "an engaged enemy shows its bar");
    }

    [UnityTest]
    public IEnumerator EnemyHitPointsStayRightAboveTheHeadAtEveryCameraAngle()
    {
        var skeleton = Skeletons[0];
        var bar = ((Component)typeof(Enemy).GetField("hitPointsDisplay", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(skeleton)).transform;
        var height = skeleton.GetComponent<CharacterController>().height * skeleton.transform.lossyScale.y;
        Teleport(Player, skeleton.transform.position + skeleton.transform.forward * 2f + Vector3.up * 0.2f);
        yield return WaitUntil(() => bar.gameObject.activeSelf, 5f, "the skeleton's bar to show");

        // A low camera looking up and a high one looking down are where a world-space "up" offset used to
        // slide the bar sideways, away from the head.
        var orbital = Object.FindAnyObjectByType<Unity.Cinemachine.CinemachineOrbitalFollow>();
        var camera = Camera.main;
        float? gap = null;
        foreach (var ring in new[] { 0.15f, 0.5f, 1f })
        foreach (var yaw in new[] { -90f, 0f, 90f, 120f })
        {
            orbital.VerticalAxis.Value = ring;
            orbital.HorizontalAxis.Value = yaw;
            for (var i = 0; i < 30; i++)
                yield return null;
            var head = camera.WorldToScreenPoint(skeleton.transform.position + Vector3.up * height);
            var onBar = camera.WorldToScreenPoint(bar.position);
            Assert.That(onBar.x - head.x, Is.EqualTo(0f).Within(1f), $"ring {ring}, yaw {yaw}: the bar slid sideways");
            gap ??= onBar.y - head.y;
            Assert.That(onBar.y - head.y, Is.EqualTo(gap.Value).Within(1f), $"ring {ring}, yaw {yaw}: the gap above the head changed");
        }
    }

    [UnityTest]
    public IEnumerator EnemyHitPointsFaceTheCameraInTheSameFrameWhileItTurns()
    {
        var skeleton = Skeletons[0];
        var bar = ((Component)typeof(Enemy).GetField("hitPointsDisplay", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(skeleton)).transform;
        Teleport(Player, skeleton.transform.position + skeleton.transform.forward * 4f + Vector3.up * 0.2f);
        yield return WaitUntil(() => bar.gameObject.activeSelf, 5f, "the skeleton's bar to show");

        // The damage text follows the enemy in screen space; it must land on this frame's projection too.
        var text = (Component)typeof(Enemy).GetField("enemyStatChangeDisplay", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(skeleton);
        var textOffset = (Vector3)text.GetType().GetField("offsetVector", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(text);

        var orbital = Object.FindAnyObjectByType<Unity.Cinemachine.CinemachineOrbitalFollow>();
        var camera = Camera.main;
        for (var i = 0; i < 20; i++) // turn the camera 12 degrees every frame, as a fast right-drag would
        {
            orbital.HorizontalAxis.Value = Mathf.Repeat(orbital.HorizontalAxis.Value + 12f + 180f, 360f) - 180f;
            yield return null;
            Assert.That(Quaternion.Angle(bar.rotation, camera.transform.rotation), Is.LessThan(0.01f), $"frame {i}: the bar lags the camera");
            var expected = camera.WorldToScreenPoint(skeleton.transform.position + textOffset);
            if (expected.z > 0f)
                Assert.That(Vector2.Distance(text.transform.position, expected), Is.LessThan(1f), $"frame {i}: the damage text lags the camera");
        }
    }

    [UnityTest]
    public IEnumerator DoorOpensOnlyAfterEverySkeletonDies()
    {
        var door = Object.FindAnyObjectByType<DoorController>();
        var skeletons = Skeletons;
        Assert.That(skeletons.Length, Is.EqualTo(2));
        Assert.That(door.IsUnlocked, Is.False);

        Kill(skeletons[0]);
        yield return null;
        Assert.That(door.IsUnlocked, Is.False, "one guardian is still alive");

        var leftDoor = (Transform)typeof(DoorController)
            .GetField("leftDoorTransform", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(door);
        var closed = leftDoor.rotation;
        Kill(skeletons[1]);
        yield return WaitSeconds(1.5f);

        Assert.That(door.IsUnlocked, Is.True);
        Assert.That(Quaternion.Angle(closed, leftDoor.rotation), Is.GreaterThan(60f), "doors did not swing open");
        Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Running), "skeletons are not the boss");
    }

    [UnityTest]
    public IEnumerator BossDeathClearsTheGame()
    {
        var boss = Boss;
        Kill(boss);
        Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Cleared));

        yield return WaitSeconds(2.5f);
        AssertResultPanel("GAME CLEAR");
        AssertDeathPose(boss.GetComponent<Animator>(), "boss");
    }

    [UnityTest]
    public IEnumerator PlayerDeathEndsTheGame()
    {
        var player = Player;
        Kill(player);

        yield return WaitSeconds(2.5f);
        Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Over));
        AssertResultPanel("GAME OVER");
        AssertDeathPose(player.GetComponent<Animator>(), "player");
    }

    [UnityTest]
    public IEnumerator PlayerDyingInTheAirStillPlaysDeath()
    {
        var player = Player;
        Teleport(player, player.transform.position + Vector3.up * 3f);
        yield return WaitSeconds(0.2f);
        Kill(player);

        yield return WaitSeconds(3f);
        AssertDeathPose(player.GetComponent<Animator>(), "player (airborne)");
    }

    [UnityTest]
    public IEnumerator SkeletonKilledMidSwingPlaysDeathOnEveryLayer()
    {
        var skeleton = Skeletons[0];
        var animator = skeleton.GetComponent<Animator>();
        Teleport(Player, skeleton.transform.position + skeleton.transform.forward * 2f + Vector3.up * 0.2f);

        yield return WaitUntil(() => animator.GetCurrentAnimatorStateInfo(1).IsName("Attack")
                                     || animator.GetCurrentAnimatorStateInfo(1).IsName("Skill"), 10f, "the skeleton to swing");
        Kill(skeleton);

        yield return WaitSeconds(1.5f);
        AssertDeathPose(animator, "skeleton");
    }

    private static IEnumerator StartBossCast()
    {
        var boss = Boss;
        SetPrivate(boss.GetComponent<AoeCastSkill>(), typeof(EnemySkill), "firstUseDelay", 0f);
        Teleport(Player, boss.transform.position + boss.transform.forward * 3f + Vector3.up * 0.2f);
        yield return WaitUntil(() => Object.FindAnyObjectByType<AoeTelegraph>(), 10f, "the boss to start casting");
    }

    [UnityTest]
    public IEnumerator BossAoeHitsAPlayerInsideTheCircle()
    {
        yield return StartBossCast();
        var hitPointsBefore = Player.Stats[Stat.HitPoints];

        yield return WaitUntil(() => !Object.FindAnyObjectByType<AoeTelegraph>(), 6f, "the cast to finish");
        yield return null;
        Assert.That(hitPointsBefore - Player.Stats[Stat.HitPoints], Is.GreaterThan(900));
    }

    [UnityTest]
    public IEnumerator BossAoeMissesAPlayerWhoLeftTheCircle()
    {
        yield return StartBossCast();
        var boss = Boss;
        Teleport(Player, boss.transform.position + boss.transform.forward * 12f + Vector3.up * 0.2f);
        yield return null;
        var hitPointsBefore = Player.Stats[Stat.HitPoints];

        yield return WaitUntil(() => !Object.FindAnyObjectByType<AoeTelegraph>(), 6f, "the cast to finish");
        yield return null;
        Assert.That(Player.Stats[Stat.HitPoints], Is.GreaterThanOrEqualTo(hitPointsBefore));
    }

    [UnityTest]
    public IEnumerator CameraStartsOnTheMiddleRingAboveThePlayer()
    {
        yield return WaitSeconds(1f);
        var orbital = Object.FindAnyObjectByType<Unity.Cinemachine.CinemachineOrbitalFollow>();
        Assert.That(orbital.VerticalAxis.GetNormalizedValue(), Is.EqualTo(0.5f).Within(0.01f));

        // The middle ring sits 1.6 above the follow target; the bug left the camera near the bottom ring.
        var height = Camera.main.transform.position.y - orbital.FollowTarget.position.y;
        Assert.That(height, Is.GreaterThan(1.2f), "camera is too low and looks up at the ceiling");
    }

    [UnityTest]
    public IEnumerator CameraIsNeverBehindAWall()
    {
        var vcam = Object.FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
        var lookAt = vcam.LookAt;
        var camera = Camera.main.transform;

        // Swing the camera all the way round; every heading must keep the player in clear view.
        var orbital = vcam.GetComponent<Unity.Cinemachine.CinemachineOrbitalFollow>();
        for (var heading = -180f; heading < 180f; heading += 30f)
        {
            orbital.HorizontalAxis.Value = heading;
            yield return WaitSeconds(0.6f);
            var toCamera = camera.position - lookAt.position;
            Assert.That(Physics.Raycast(lookAt.position, toCamera.normalized, toCamera.magnitude - 0.05f, 1 << 9,
                QueryTriggerInteraction.Ignore), Is.False, $"a wall hides the player at heading {heading}");
        }
    }

    [UnityTest]
    public IEnumerator MusicFollowsTheFightAndEndsWithAStinger()
    {
        var audio = AudioManager.Instance;
        yield return null;
        Assert.That(audio.CurrentMusic.name, Is.EqualTo("Dungeon Exploration"));

        GameManager.Instance.SetEngaged(Skeletons[0], true);
        yield return null;
        Assert.That(audio.CurrentMusic.name, Is.EqualTo("Skeleton Fight"));

        GameManager.Instance.SetEngaged(Boss, true);
        yield return null;
        Assert.That(audio.CurrentMusic.name, Is.EqualTo("Boss Fight"));

        Kill(Boss);
        yield return null;
        Assert.That(audio.CurrentMusic.name, Is.EqualTo("Game Clear"));
    }

    [UnityTest]
    public IEnumerator SoundEffectsReuseAFixedPoolOfVoices()
    {
        var audio = AudioManager.Instance;
        var before = audio.GetComponentsInChildren<AudioSource>().Length;
        for (var i = 0; i < 40; i++)
            Assert.That(audio.Play(Sfx.UiClick), Is.Not.Null);
        yield return null;
        Assert.That(audio.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(before));
    }

    [UnityTest]
    public IEnumerator BossKilledLateInTheCastStillFalls()
    {
        // Cancelling the cast starts a Casting -> Idle blend; the death must still win over it.
        yield return StartBossCast();
        yield return WaitSeconds(1.5f);
        var boss = Boss;
        Kill(boss);

        yield return WaitSeconds(2f);
        AssertDeathPose(boss.GetComponent<Animator>(), "boss (killed 1.5 s into the cast)");
    }

    [UnityTest]
    public IEnumerator ChangingTargetMovesTheMarker()
    {
        var player = Player;
        var first = Skeletons[0].GetComponent<PlayerTarget>();
        var second = Skeletons[1].GetComponent<PlayerTarget>();

        player.SelectTarget(first.gameObject);
        yield return null;
        player.SelectTarget(second.gameObject);
        yield return null;

        Assert.That(first.TargetIndicator.gameObject.activeSelf, Is.False, "old target still shows the marker");
        Assert.That(second.TargetIndicator.gameObject.activeSelf, Is.True);
    }

    [UnityTest]
    public IEnumerator PlayerStopsRunningWhenTheResultIsDecided()
    {
        var animator = Player.GetComponent<Animator>();
        yield return WaitSeconds(0.5f); // let the player settle on the ground
        animator.SetInteger("MovementMode", 2); // as if running when the boss falls

        GameManager.Instance.EndGame(GameState.Cleared);
        yield return null;
        yield return null;

        Assert.That(animator.GetInteger("MovementMode"), Is.Zero, "the player keeps running after the result");
    }

    [UnityTest]
    public IEnumerator CameraLetsGoOnceTheResultIsShown()
    {
        var controller = Object.FindAnyObjectByType<PlayerCameraController>();
        SetPrivate(controller, typeof(PlayerCameraController), "isRotating", true);
        Cursor.lockState = CursorLockMode.Locked;

        GameManager.Instance.EndGame(GameState.Over);
        yield return null;

        Assert.That(typeof(PlayerCameraController).GetField("isRotating", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(controller), Is.False);
        Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
    }

    [UnityTest]
    public IEnumerator BossKilledMidCastLeavesNoMarkerBehind()
    {
        yield return StartBossCast();
        var boss = Boss;
        Kill(boss);
        yield return null;
        yield return null;

        Assert.That(Object.FindAnyObjectByType<AoeTelegraph>(), Is.Null);
        Assert.That(GameManager.Instance.State, Is.EqualTo(GameState.Cleared));
        yield return WaitSeconds(1.5f);
        AssertDeathPose(boss.GetComponent<Animator>(), "boss (mid-cast)");
    }
}
