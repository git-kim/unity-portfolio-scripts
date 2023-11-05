using System.Collections.Generic;
using FluentBuilderPattern;

public interface IDamageable
{
    int ID { get; }

    bool IsDead { get; set; }

    void IncreaseStat(Stat stat, int increment, bool shouldShowDigits = false, bool additionalOption = false);

    void DecreaseStat(Stat stat, int decrement, bool shouldShowDigits = false, bool additionalOption = false);

    void UpdateStatBars();

    Dictionary<int, KeyValuePair<Stat, int>> ActiveBuffEffects { get; set; }
}
