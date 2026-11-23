using Characters.Handlers;
using Characters.StatisticsScripts;
using NUnit.Framework;
using UnityEngine;

public class StatChangeHandlerTests
{
    private GameObject go;
    private StatChangeHandler handler;
    private Statistics stats;
    private int deaths;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("Stat Test");
        handler = go.AddComponent<StatChangeHandler>();
        stats = new StatisticsBuilder()
            .SetBaseValue(Stat.HitPoints, 100)
            .SetBaseValue(Stat.MaximumHitPoints, 100)
            .SetBaseValue(Stat.ManaPoints, 50)
            .SetBaseValue(Stat.MaximumManaPoints, 50)
            .SetBaseValue(Stat.MeleeDefense, 10)
            .SetBaseValue(Stat.MagicDefense, 20);
        deaths = 0;
        handler.Initialize(new StatChangeHandler.InitializationContext
        {
            identifier = 1,
            stats = stats,
            onHitPointsBecomeZero = () => deaths++
        });
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(go);

    [Test]
    public void DeathFiresOnceWhenHitPointsReachZero()
    {
        handler.DecreaseStat(Stat.HitPoints, 100);

        Assert.That(deaths, Is.EqualTo(1));
        Assert.That(handler.HasZeroHitPoints, Is.True);
        Assert.That(stats[Stat.ManaPoints], Is.Zero, "dying drains MP");
    }

    [Test]
    public void OverkillAndLaterHitsDoNotFireDeathAgain()
    {
        handler.DecreaseStat(Stat.HitPoints, 5000);
        handler.DecreaseStat(Stat.HitPoints, 10);

        Assert.That(deaths, Is.EqualTo(1));
        Assert.That(stats[Stat.HitPoints], Is.Zero);
    }

    [Test]
    public void DamageOverTimeKillsExactlyOnce()
    {
        handler.AddStatChangingEffect(3, new StatChangeHandler.StatChangingEffectData
        {
            type = StatChangeHandler.StatChangingEffectType.AppliedPerTick,
            stat = Stat.HitPoints,
            value = -60
        });

        handler.ApplyActiveStatChangingEffects();
        handler.ApplyActiveStatChangingEffects();
        handler.ApplyActiveStatChangingEffects();

        Assert.That(deaths, Is.EqualTo(1));
        Assert.That(stats[Stat.HitPoints], Is.Zero);
    }

    [Test]
    public void DeadCharacterCannotBeHealed()
    {
        handler.DecreaseStat(Stat.HitPoints, 100);
        handler.IncreaseStat(Stat.HitPoints, 50);

        Assert.That(stats[Stat.HitPoints], Is.Zero);
        Assert.That(handler.HasZeroHitPoints, Is.True);
    }

    [Test]
    public void HealingIsCappedAtMaximum()
    {
        handler.DecreaseStat(Stat.HitPoints, 30);
        handler.IncreaseStat(Stat.HitPoints, 500);

        Assert.That(stats[Stat.HitPoints], Is.EqualTo(100));
    }

    [Test]
    public void EffectiveDamageSubtractsDefenseAndIsNeverNegative()
    {
        Assert.That(handler.GetEffectiveDamage(100, true, 1f), Is.EqualTo(90));
        Assert.That(handler.GetEffectiveDamage(100, false, 1f), Is.EqualTo(80));
        Assert.That(handler.GetEffectiveDamage(5, false, 1f), Is.Zero);
    }
}
