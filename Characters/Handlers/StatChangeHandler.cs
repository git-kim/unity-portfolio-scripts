using Characters.StatisticsScripts;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UserInterface;

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

            if (hitAndManaPointsDisplay)
            {
                UpdateHitPointsDisplay();
                UpdateManaPointsDisplay();
            }
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

                        if (hitAndManaPointsDisplay)
                            UpdateHitPointsDisplay();
                    }
                    break;
                case Stat.ManaPoints:
                    {
                        var maximumValue = stats[Stat.MaximumManaPoints];
                        stats[stat] = Mathf.Min(value, maximumValue);

                        if (hitAndManaPointsDisplay)
                            UpdateManaPointsDisplay();
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
                        if (value == 0 && !HasZeroHitPoints)
                        {
                            DecreaseStat(Stat.ManaPoints, stats[Stat.ManaPoints]);
                            HasZeroHitPoints = true;
                        }

                        if (hitAndManaPointsDisplay)
                            UpdateHitPointsDisplay();
                    }
                    break;
                case Stat.ManaPoints:
                    {
                        if (hitAndManaPointsDisplay)
                            UpdateManaPointsDisplay();
                    }
                    break;
            }
        }

        public void UpdateHitPointsDisplay()
        {
            var currentPoints = stats[Stat.HitPoints];
            hitAndManaPointsDisplay.UpdateHitPointsBar(currentPoints, stats[Stat.MaximumHitPoints]);
            hitAndManaPointsDisplay.UpdateHitPointsText(currentPoints);
        }

        private void UpdateManaPointsDisplay()
        {
            var currentPoints = stats[Stat.ManaPoints];
            hitAndManaPointsDisplay.UpdateManaPointsBar(currentPoints, stats[Stat.MaximumManaPoints]);
            hitAndManaPointsDisplay.UpdateManaPointsText(currentPoints);
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

        public void AddStatChangingEffect(int buffIndex, in StatChangingEffectData data)
        {
            if (!ActiveStatChangingEffects.ContainsKey(buffIndex))
            {
                ActiveStatChangingEffects.Add(buffIndex, data);
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

        public void RemoveStatChangingEffect(int buffIndex)
        {
            if (ActiveStatChangingEffects.TryGetValue(buffIndex, out var data)
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

            ActiveStatChangingEffects.Remove(buffIndex);
        }

        public bool HasStatChangingEffect(int buffIndex)
        {
            return ActiveStatChangingEffects.ContainsKey(buffIndex);
        }
    }
}