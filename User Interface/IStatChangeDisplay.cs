public interface IStatChangeDisplay
{
    void ShowHitPointsChange(int change, bool isDecrement, in string actionName);

    void ShowHitPointsChangeOverTime(int change, bool isDecrement = false);

    void ShowBuffStart(int buffIdentifier, float effectTime);

    void ShowBuffEnd(int buffIdentifier);

    void RemoveAllDisplayingBuffs();
}