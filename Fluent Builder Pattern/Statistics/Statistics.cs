using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluentBuilderPattern
{
    public enum Stat
    {
        hP, maxHP, reciprocalOfMaxHP, mP, maxMP, reciprocalOfMaxMP,
        meleeAttackPower, magicAttackPower, meleeDefensePower, magicDefensePower,
        hPRestoringPower, mPRestoringPower
    }

    public class Statistics : Dictionary<Stat, int> {}
}