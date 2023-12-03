using GameData;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Characters.Handlers
{
    public class StatChangeHandler : MonoBehaviour
    {
        public enum StatChangingEffectType
        {
            Temporal,
            AppliedPerTick
        }

        public struct StatChangingEffectData
        {
            public StatChangingEffectType type;
            public Stat stat;
            public int value;
        }

        public struct InitializationContext
        {
            public int identifier;
            public Statistics stats;
            public HitAndManaPointsDisplay hitAndManaPointsDisplay;
            public IStatChangeDisplay statChangeDisplay;
            public UnityAction onHitPointsBecomeZero;
        }

        public int Identifier { get; private set; }

        private Statistics stats;
        private HitAndManaPointsDisplay hitAndManaPointsDisplay;
        private IStatChangeDisplay statChangeDisplay;

        private event UnityAction OnHitPointsBecomeZero;
        private bool hasZeroHitPoints;
        public bool HasZeroHitPoints
        {
            get => hasZeroHitPoints;
            private set
            {
                hasZeroHitPoints = value;
                if (value)
                {
                    OnHitPointsBecomeZero?.Invoke();
                }
            }
        }

        public Dictionary<int, StatChangingEffectData> ActiveStatChangingEffects { get; set; }

        public void Initialize(in InitializationContext context)
        {
            ActiveStatChangingEffects = new Dictionary<int, StatChangingEffectData>(5);

            Identifier = context.identifier;
            stats = context.stats;
            hitAndManaPointsDisplay = context.hitAndManaPointsDisplay;
            statChangeDisplay = context.statChangeDisplay;
            OnHitPointsBecomeZero = context.onHitPointsBecomeZero;

            UpdateHitPointsBar();
            UpdateManaPointsBar();
        }

        public void IncreaseStat(Stat stat, int increment)
        {
            var value = stats[stat] += increment;

            switch (stat)
            {
                case Stat.HitPoints:
                    {
                        var maximumValue = stats[Stat.MaximumHitPoints];
                        stats[stat] = Mathf.Min(value, maximumValue);
                        UpdateHitPointsBar();
                    }
                    break;
                case Stat.ManaPoints:
                    {
                        var maximumValue = stats[Stat.MaximumManaPoints];
                        stats[stat] = Mathf.Min(value, maximumValue);
                        UpdateManaPointsBar();
                    }
                    break;
            }
        }

        public void DecreaseStat(Stat stat, int decrement)
        {
            var value = stats[stat] = Mathf.Max(stats[stat] - decrement, 0);

            switch (stat)
            {
                case Stat.HitPoints:
                    {
                        if (value == 0 && !hasZeroHitPoints)
                        {
                            DecreaseStat(Stat.ManaPoints, stats[Stat.ManaPoints]);
                            hasZeroHitPoints = true;
                        }

                        UpdateHitPointsBar();
                    }
                    break;
                case Stat.ManaPoints:
                    {
                        UpdateManaPointsBar();
                    }
                    break;
            }
        }

        private void UpdateHitPointsBar()
        {
            hitAndManaPointsDisplay.SelfOrNull()?
                .UpdateHitPointsBar(stats[Stat.HitPoints], stats[Stat.MaximumHitPoints]);
        }

        private void UpdateManaPointsBar()
        {
            hitAndManaPointsDisplay.SelfOrNull()?
                           .UpdateManaPointsBar(stats[Stat.ManaPoints], stats[Stat.MaximumManaPoints]);
        }

        public void ShowHitPointsChange(int change, bool isDecrement, string actionName)
        {
            statChangeDisplay.SelfOrNull()?
                .ShowHitPointsChange(change, isDecrement, actionName);
        }

        public int GetEffectiveDamage(int desiredDamage, bool isMelee, float adjustmentFactor)
        {
            var damage = Mathf.RoundToInt(desiredDamage * adjustmentFactor);

            if (isMelee)
            {
                damage -= stats[Stat.MeleeDefense];
            }
            else // magic
            {
                damage -= stats[Stat.MagicDefense];
            }

            return Mathf.Max(0, damage);
        }

        public void ApplyActiveStatChangingEffects()
        {
            foreach (var data in ActiveStatChangingEffects.Values)
            {
                if (data.type == StatChangingEffectType.AppliedPerTick)
                {
                    if (data.value < 0)
                    {
                        DecreaseStat(data.stat, -data.value);
                        if (data.stat == Stat.HitPoints)
                            ShowHitPointsChange(-data.value, true, null);
                    }
                    else
                    {
                        IncreaseStat(data.stat, data.value);
                        if (data.stat == Stat.HitPoints)
                            ShowHitPointsChange(data.value, false, null);
                    }
                }
            }
        }

        public void AddStatChangingEffect(int buffIdentifier, in StatChangingEffectData data)
        {
            if (!ActiveStatChangingEffects.ContainsKey(buffIdentifier))
            {
                ActiveStatChangingEffects.Add(buffIdentifier, data);
            }

            if (data.type == StatChangingEffectType.Temporal)
            {
                if (data.value < 0)
                {
                    DecreaseStat(data.stat, -data.value);
                }
                else
                {
                    IncreaseStat(data.stat, data.value);
                }
            }
        }

        public void RemoveStatChangingEffect(int buffIdentifier)
        {
            if (ActiveStatChangingEffects.TryGetValue(buffIdentifier, out var data)
                && data.type == StatChangingEffectType.Temporal)
            {
                if (data.value < 0)
                {
                    IncreaseStat(data.stat, -data.value);
                }
                else
                {
                    DecreaseStat(data.stat, data.value);
                }
            }

            ActiveStatChangingEffects.Remove(buffIdentifier);
        }

        public bool HasStatChangingEffect(int buffIdentifier)
        {
            return ActiveStatChangingEffects.ContainsKey(buffIdentifier);
        }
    }
}