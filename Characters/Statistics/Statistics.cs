namespace GameData
{
    public class Statistics
    {
        private readonly int[] baseValues;
        private readonly int[] offsets;

        internal Statistics(int[] values)
        {
            this.baseValues = values;
            offsets = new int[values.Length];
        }

        // Indexer definition
        public int this[Stat stat]
        {
            get { return baseValues[(int)stat] + offsets[(int)stat]; }
            set { offsets[(int)stat] = value - baseValues[(int)stat]; }
        }

        public void Reset()
        {
            var offsetsLength = offsets.Length;

            for (var i = 0; i < offsetsLength; ++i)
            {
                offsets[i] = 0;
            }
        }
    }
}