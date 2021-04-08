using System.Collections.Generic;
using CommandPattern;

public class Actions: List<ActionInfo>
{
}

//public enum ActionCoolDownType
//{
//    Global,
//    OffGlobal
//}

public enum ActionTargetType
{
    Self,
    NonSelf
}

/// <summary>
/// 액션 정보(range는 사용 가능 최대 거리, areaOfEffectRadius는 액션이 적용되는 영역의 반경을 나타낸다.)
/// </summary>
public class ActionInfo
{
    public int id;
    public ICommand actionCommand;
    public ActionTargetType targetType;
    //public ActionCoolDownType coolDownType;
    public float castTime;
    public float coolDownTime;
    public float invisibleGlobalCoolDownTime;
    public float range;
    public float areaOfEffectRadius;
    public int mPCost;
    public readonly string name;
    public readonly string description;
    public bool canIgnoreVisibleGlobalCoolDownTime;

    public ActionInfo(int id, ICommand actionCommand, ActionTargetType targetType, //ActionCoolDownType coolDownType,
        float castTime, float coolDownTime = 2f, float invisibleGlobalCoolDownTime = 0.5f,
        float range = 0f, float areaOfEffectRadius = 0f,
        int mPCost = 0,
        string name = "", string description = "", bool canIgnoreVisibleGlobalCoolDownTime = false)
    {
        this.id = id;
        this.actionCommand = actionCommand;
        this.targetType = targetType;
        //this.coolDownType = coolDownType;
        this.castTime = castTime;
        this.coolDownTime = coolDownTime;
        this.invisibleGlobalCoolDownTime = invisibleGlobalCoolDownTime;
        this.range = range;
        this.areaOfEffectRadius = areaOfEffectRadius;
        this.mPCost = mPCost;
        this.name = name;
        this.description = description;
        this.canIgnoreVisibleGlobalCoolDownTime = canIgnoreVisibleGlobalCoolDownTime;
    }
}
