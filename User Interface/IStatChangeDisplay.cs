namespace UserInterface
{
    public interface IStatChangeDisplay
    {
        void ShowHitPointsChange(int change, bool isDecrement, in string actionName);

        void ShowHitPointsChangeOverTime(int change, bool isDecrement = false);

        void ShowBuffStart(int buffIndex, float effectTime);

        void ShowBuffEnd(int buffIndex);

        void RemoveAllDisplayingBuffs();
    }
}