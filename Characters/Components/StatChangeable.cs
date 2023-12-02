using GameData;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Events;

namespace Characters.Components
{
    public class StatChangeable : MonoBehaviour
    {
        public struct InitializationContext
        {
            public int Identifier;
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

        public Dictionary<int, KeyValuePair<Stat, int>> ActiveStatChangingEffects { get; set; }

        public void Initialize(InitializationContext context)
        {
            Identifier = context.Identifier;
            stats = context.stats;
            hitAndManaPointsDisplay = context.hitAndManaPointsDisplay;
            statChangeDisplay = context.statChangeDisplay;
            OnHitPointsBecomeZero = context.onHitPointsBecomeZero;
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
    }
}