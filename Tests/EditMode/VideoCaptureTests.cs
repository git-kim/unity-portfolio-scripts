using System.Collections;
using System.Linq;
using Characters;
using Characters.Handlers;
using Characters.StatisticsScripts;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using VideoCapture;

public class VideoCaptureTests
{
    [Test]
    public void SrtGroupsCloseEventsSkipsRepeatsAndEndsBeforeTheNextCue()
    {
        var srt = Captions.ToSrt(new[]
        {
            new GameEvent(0f, "start"),
            new GameEvent(12.25f, "skeleton-down", "2/2"),
            new GameEvent(12.25f, "door-open"),
            new GameEvent(14f, "aoe-cast"),
            new GameEvent(20f, "aoe-cast"),
            new GameEvent(21f, "unknown-event"),
        }, korean: false);

        Assert.That(srt, Is.EqualTo(
            "1\n00:00:00,000 --> 00:00:03,500\nMigrated from Unity 2022.3 to Unity 6\n\n" +
            "2\n00:00:12,250 --> 00:00:13,950\nSkeleton warrior down (2/2)\nThe boss door opens once both skeleton warriors fall\n\n" +
            "3\n00:00:14,000 --> 00:00:17,500\nBoss AOE: leave the circle before the cast ends to dodge\n\n")); // the repeat cast gets none
    }

    [Test]
    public void StampRollsOverIntoHours() => Assert.That(Captions.Stamp(3725.0049f), Is.EqualTo("01:02:05,005"));

    [Test]
    public void EventListRoundTripsThroughTsv()
    {
        var events = new[] { new GameEvent(0f, "start"), new GameEvent(32.75f, "skeleton-down", "1/2") };
        var back = Captions.FromTsv(Captions.ToTsv(events).Replace("\n", "\r\n")); // as saved on Windows
        Assert.That(back.Select(e => (e.Time, e.Id, e.Detail)), Is.EqualTo(events.Select(e => (e.Time, e.Id, e.Detail))));
    }

    [UnityTest]
    public IEnumerator WatcherLogsKillsDoorAndResult()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Scene 1.unity");
        yield return new EnterPlayMode();

        var watcher = new GameEventWatcher();
        watcher.Begin();
        var enemies = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).OrderBy(e => e.IsBoss).ToArray();
        foreach (var enemy in enemies)
        {
            enemy.GetComponent<StatChangeHandler>().DecreaseStat(Stat.HitPoints, 1_000_000);
            for (var i = 0; i < 10; i++)
                yield return null;
        }
        watcher.End();
        var ids = watcher.Events.Select(e => e.Id).ToArray();

        yield return new ExitPlayMode();

        Assert.That(ids, Is.EqualTo(new[] { "start", "skeleton-down", "skeleton-down", "door-open", "boss-down", "cleared" }));
        Assert.That(watcher.Events[2].Detail, Is.EqualTo("2/2"));
        Assert.That(watcher.Events.Select(e => e.Time), Is.Ordered);
    }

    [UnityTest]
    public IEnumerator AutopilotPlaysTheWholeRun()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Scene 1.unity");
        yield return new EnterPlayMode();

        Time.captureFramerate = 60; // as during a recording
        foreach (var animator in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include))
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; // nothing renders in batch mode
        var watcher = new GameEventWatcher();
        var autopilot = new Autopilot();
        watcher.Begin();
        autopilot.Start(null);
        for (var lastFrame = Time.frameCount + 60 * 400; autopilot.IsRunning && Time.frameCount < lastFrame;)
            yield return null;
        autopilot.Stop();
        watcher.End();
        Time.captureFramerate = 0;
        var events = string.Join("\n", watcher.Events.Select(e => $"{e.Time:0.0} {e.Id} {e.Detail}"));
        Debug.Log("AUTOPILOT LOG\n" + string.Join("\n", autopilot.Log) + "\nEVENTS\n" + events);
        var ids = watcher.Events.Select(e => e.Id).ToList();

        yield return new ExitPlayMode();

        Assert.That(ids, Does.Contain("cleared"), events);
        Assert.That(ids, Is.SupersetOf(new[] { "first-battle", "heal", "off-gcd", "door-open", "boss-engaged", "aoe-cast", "aoe-dodged", "boss-down" }), events);
        Assert.That(ids.Count(id => id == "skeleton-down"), Is.EqualTo(2), events);
        Assert.That(ids, Does.Not.Contain("aoe-hit"), events);
    }
}
