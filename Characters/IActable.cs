using GameData;

public interface IActable
{
    int ActionToTake { get; set; }
    int ActionBeingTaken { get; set; }
    bool IsCasting { get; set; }

    float GlobalCoolDownTime { get; }
    float VisibleGlobalCoolDownTime { get; set; }
    float InvisibleGlobalCoolDownTime { get; set; }

    float SqrDistanceFromCurrentTarget { get; }

    CastingBarDisplay CastingBarDisplay { get; }

    Actions ActionCommands { get; }

    GameData.Statistics Stats { get; }
}
