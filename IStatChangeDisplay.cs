public interface IStatChangeDisplay
{
    void ShowHPChange(int change, bool isDecrement, in string actionName);

    void ShowHPChangeOverTime(int change, bool isDecrement = false);

    void ShowBuffStart(int buffID, float effectTime);

    void ShowBuffEnd(int buffID);

    void RemoveAllDisplayingBuffs();
}