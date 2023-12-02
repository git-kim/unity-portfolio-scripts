public interface IStatChangeDisplay
{
    void ShowHitPointsChange(int change, bool isDecrement, in string actionName);

    void ShowHitPointsChangeOverTime(int change, bool isDecrement = false);

    void ShowBuffStart(int buffID, float effectTime);

    void ShowBuffEnd(int buffID);

    void RemoveAllDisplayingBuffs();
}