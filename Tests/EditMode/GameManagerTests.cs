using System.Collections.Generic;
using System.Reflection;
using Characters;
using Enums;
using Managers;
using NUnit.Framework;
using UnityEngine;

public class GameManagerTests
{
    private GameObject managerObject;
    private GameManager manager;
    private readonly List<GameState> results = new List<GameState>();

    [SetUp]
    public void SetUp()
    {
        managerObject = new GameObject("GameManager");
        manager = managerObject.AddComponent<GameManager>();
        results.Clear();
        manager.GameEnded += results.Add;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Include))
            Object.DestroyImmediate(enemy.gameObject);
        Object.DestroyImmediate(managerObject);
    }

    private static Enemy NewEnemy(bool isBoss)
    {
        var enemy = new GameObject(isBoss ? "Boss" : "Minion").AddComponent<Enemy>();
        typeof(Enemy).GetField("isBoss", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(enemy, isBoss);
        return enemy;
    }

    [Test]
    public void FirstResultWins()
    {
        manager.EndGame(GameState.Over);
        manager.EndGame(GameState.Cleared);

        Assert.That(manager.State, Is.EqualTo(GameState.Over));
        Assert.That(results, Is.EqualTo(new[] { GameState.Over }));
    }

    [Test]
    public void RunningIsNotAResult()
    {
        manager.EndGame(GameState.Running);

        Assert.That(manager.State, Is.EqualTo(GameState.Running));
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void BossDeathClearsTheGame()
    {
        var died = new List<Enemy>();
        manager.EnemyDied += died.Add;
        var boss = NewEnemy(true);

        manager.NotifyEnemyDied(boss);

        Assert.That(died, Is.EqualTo(new[] { boss }));
        Assert.That(manager.State, Is.EqualTo(GameState.Cleared));
    }

    [Test]
    public void MinionDeathDoesNotEndTheGame()
    {
        manager.NotifyEnemyDied(NewEnemy(false));

        Assert.That(manager.State, Is.EqualTo(GameState.Running));
        Assert.That(results, Is.Empty);
    }

    [Test]
    public void BattleLastsWhileAnyEnemyIsEngaged()
    {
        var a = NewEnemy(false);
        var b = NewEnemy(false);

        manager.SetEngaged(a, true);
        manager.SetEngaged(b, true);
        manager.SetEngaged(a, false);
        Assert.That(manager.IsInBattle, Is.True);

        manager.NotifyEnemyDied(b);
        Assert.That(manager.IsInBattle, Is.False);
    }
}
