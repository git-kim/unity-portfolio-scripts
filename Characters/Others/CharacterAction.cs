using System.Collections.Generic;
using Characters.CharacterActionCommands;

public class CharacterActions: List<CharacterAction>
{
}

public enum CharacterActionTargetType
{
    Self,
    NonSelf
}

public class CharacterAction
{
    public struct CreationContext
    {
        public int id;
        public CharacterActionCommand actionCommand;
        public CharacterActionTargetType targetType;
        public float castTime;
        public float coolDownTime;
        public float invisibleGlobalCoolDownTime;
        public float range;
        public float areaOfEffectRadius;
        public int manaPointsCost;
        public string name;
        public string description;
        public bool canIgnoreVisibleGlobalCoolDownTime;

        public CreationContext(int id, CharacterActionCommand actionCommand, CharacterActionTargetType targetType,
            float castTime, float coolDownTime = 2f, float invisibleGlobalCoolDownTime = 0.5f,
            float range = 0f, float areaOfEffectRadius = 0f, int manaPointsCost = 0,
            string name = null, string description = null, bool canIgnoreVisibleGlobalCoolDownTime = false)
        {
            this.id = id;
            this.actionCommand = actionCommand;
            this.targetType = targetType;
            this.castTime = castTime;
            this.coolDownTime = coolDownTime;
            this.invisibleGlobalCoolDownTime = invisibleGlobalCoolDownTime;
            this.range = range;
            this.areaOfEffectRadius = areaOfEffectRadius;
            this.manaPointsCost = manaPointsCost;
            this.name = name ?? string.Empty;
            this.description = description ?? string.Empty;
            this.canIgnoreVisibleGlobalCoolDownTime = canIgnoreVisibleGlobalCoolDownTime;
        }
    }

    public int id;
    public CharacterActionCommand actionCommand;
    public CharacterActionTargetType targetType;
    public float castTime;
    public float coolDownTime;
    public float invisibleGlobalCoolDownTime;
    public float range;
    public float areaOfEffectRadius;
    public int manaPointsCost;
    public readonly string name;
    public readonly string description;
    public bool canIgnoreVisibleGlobalCoolDownTime;

    public CharacterAction(in CreationContext context)
    {
        id = context.id;
        actionCommand = context.actionCommand;
        targetType = context.targetType;
        castTime = context.castTime;
        coolDownTime = context.coolDownTime;
        invisibleGlobalCoolDownTime = context.invisibleGlobalCoolDownTime;
        range = context.range;
        areaOfEffectRadius = context.areaOfEffectRadius;
        manaPointsCost = context.manaPointsCost;
        name = context.name;
        description = context.description;
        canIgnoreVisibleGlobalCoolDownTime = context.canIgnoreVisibleGlobalCoolDownTime;
    }
}