namespace GameData
{
    /// <summary>
    /// Used to create a stat collection using a fluent builder pattern.
    /// </summary>
    public class StatisticsBuilder
    {
        private readonly int[] values;

        public StatisticsBuilder() => values = new int[11];

        public StatisticsBuilder SetBaseValue(Stat stat, int value)
        {
            values[(int)stat] = value;
            return this;
        }

        // Implicit operator definition
        public static implicit operator Statistics(StatisticsBuilder instance)
        {
            return new Statistics(instance.values);
        }
    }
}