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
            if (!statistics.ContainsKey(Stat.hP)) statistics.Add(Stat.hP, hP);
            return this;
        }

        public StatisticsBuilder SetMaxHP(int maxHP)
        {
            if (!statistics.ContainsKey(Stat.maxHP)) statistics.Add(Stat.maxHP, maxHP);
            return this;
        }

        public StatisticsBuilder SetMP(int mP)
        {
            if (!statistics.ContainsKey(Stat.mP)) statistics.Add(Stat.mP, mP);
            return this;
        }

        public StatisticsBuilder SetMaxMP(int maxMP)
        {
            if (!statistics.ContainsKey(Stat.maxMP)) statistics.Add(Stat.maxMP, maxMP);
            return this;
        }

        public StatisticsBuilder SetMeleeAttackPower(int meleeAttackPower)
        {
            if (!statistics.ContainsKey(Stat.meleeAttackPower)) statistics.Add(Stat.meleeAttackPower, meleeAttackPower);
            return this;
        }

        public StatisticsBuilder SetMagicAttackPower(int magicAttackPower)
        {
            if (!statistics.ContainsKey(Stat.magicAttackPower)) statistics.Add(Stat.magicAttackPower, magicAttackPower);
            return this;
        }

        public StatisticsBuilder SetMeleeDefensePower(int meleeDefensePower)
        {
            if (!statistics.ContainsKey(Stat.meleeDefensePower)) statistics.Add(Stat.meleeDefensePower, meleeDefensePower);
            return this;
        }

        public StatisticsBuilder SetMagicDefensePower(int magicDefensePower)
        {
            if (!statistics.ContainsKey(Stat.magicDefensePower)) statistics.Add(Stat.magicDefensePower, magicDefensePower);
            return this;
        }

        public StatisticsBuilder SetHPRestoringPower(int hPRestoringPower)
        {
            if (!statistics.ContainsKey(Stat.hPRestoringPower)) statistics.Add(Stat.hPRestoringPower, hPRestoringPower);
            return this;
        }

        public StatisticsBuilder SetMPRestoringPower(int mPRestoringPower)
        {
            if (!statistics.ContainsKey(Stat.mPRestoringPower)) statistics.Add(Stat.mPRestoringPower, mPRestoringPower);
            return this;
        }

        /// <summary>
        /// 암시 형 변환(StatisticsBuilder to Statistics) 연산자 사용 시 호출될 함수이다.
        /// </summary>
        /// <returns>Statistics 객체</returns>
        public Statistics FinishBuilding => statistics;

        /// <summary>
        /// Statistics 변수에 StatisticsBuilder 객체가 대입되려고 하면 Statistics 객체가 대신 대입되게 하는 암시 형 변환 연산자
        /// </summary>
        /// <param name="statisticsBuilderInstance">StatisticsBuilder 객체</param>
        public static implicit operator Statistics(StatisticsBuilder statisticsBuilderInstance)
            => statisticsBuilderInstance.FinishBuilding;
    }
}
