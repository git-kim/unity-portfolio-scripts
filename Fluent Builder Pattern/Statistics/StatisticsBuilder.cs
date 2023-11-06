namespace FluentBuilderPattern
{
    public class StatisticsBuilder
    {
        private readonly Statistics statistics;

        public StatisticsBuilder()
        {
            statistics = new Statistics();
        }

        public StatisticsBuilder SetHP(int hP)
        {
            statistics.TryAdd(Stat.HP, hP);
            return this;
        }

        public StatisticsBuilder SetMaxHP(int maxHP)
        {
            statistics.TryAdd(Stat.MaxHP, maxHP);
            return this;
        }

        public StatisticsBuilder SetMP(int mP)
        {
            statistics.TryAdd(Stat.MP, mP);
            return this;
        }

        public StatisticsBuilder SetMaxMP(int maxMP)
        {
            statistics.TryAdd(Stat.MaxMP, maxMP);
            return this;
        }

        public StatisticsBuilder SetMeleeAttackPower(int meleeAttackPower)
        {
            statistics.TryAdd(Stat.MeleeAttackPower, meleeAttackPower);
            return this;
        }

        public StatisticsBuilder SetMagicAttackPower(int magicAttackPower)
        {
            statistics.TryAdd(Stat.MagicAttackPower, magicAttackPower);
            return this;
        }

        public StatisticsBuilder SetMeleeDefensePower(int meleeDefensePower)
        {
            statistics.TryAdd(Stat.MeleeDefensePower, meleeDefensePower);
            return this;
        }

        public StatisticsBuilder SetMagicDefensePower(int magicDefensePower)
        {
            statistics.TryAdd(Stat.MagicDefensePower, magicDefensePower);
            return this;
        }

        public StatisticsBuilder SetHPRestoringPower(int hPRestoringPower)
        {
            statistics.TryAdd(Stat.HPRestoringPower, hPRestoringPower);
            return this;
        }

        public StatisticsBuilder SetMPRestoringPower(int mPRestoringPower)
        {
            statistics.TryAdd(Stat.MPRestoringPower, mPRestoringPower);
            return this;
        }

        /// <summary>
        /// 암시 형 변환(StatisticsBuilder to Statistics) 연산자 사용 시 호출될 함수이다.
        /// </summary>
        /// <returns>Statistics 객체</returns>
        private Statistics FinishBuilding => statistics;

        /// <summary>
        /// Statistics 변수에 StatisticsBuilder 객체가 대입되려고 하면 Statistics 객체가 대신 대입되게 하는 암시 형 변환 연산자
        /// </summary>
        /// <param name="statisticsBuilderInstance">StatisticsBuilder 객체</param>
        public static implicit operator Statistics(StatisticsBuilder statisticsBuilderInstance)
            => statisticsBuilderInstance.FinishBuilding;
    }
}
