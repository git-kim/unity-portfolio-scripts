using System.Collections.Generic;

namespace FluentBuilderPattern
{
    public enum Stat
    {
        HP, MaxHP, MP, MaxMP,
        MeleeAttackPower, MagicAttackPower, MeleeDefensePower, MagicDefensePower,
        HPRestoringPower, MPRestoringPower
    }

    public class Statistics : Dictionary<Stat, int> {}
}