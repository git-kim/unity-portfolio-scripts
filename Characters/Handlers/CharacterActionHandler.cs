using Characters.StatisticsScripts;
using UnityEngine;
using UserInterface;

namespace Characters.Handlers
{
    public class CharacterActionHandler : MonoBehaviour
    {
        public struct InitializationContext
        {
            public int actionToTake;
            public int actionBeingTaken;
            public bool isCasting;

            public float globalCoolDownTime;
            public float visibleGlobalCoolDownTime;
            public float invisibleGlobalCoolDownTime;

            public float sqrDistanceFromCurrentTarget;

            public CastingBarDisplay castingBarDisplay;

            public CharacterActions characterActions;

            public Statistics stats;
        }

        public GameObject CurrentTarget { get; protected set; } = null;
        public GameObject RecentTarget { get; protected set; } = null;

        public int ActionToTake { get; set; }
        public int ActionBeingTaken { get; set; }
        public bool IsCasting { get; set; }

        public float GlobalCoolDownTime { get; private set; }
        public float VisibleGlobalCoolDownTime { get; set; }
        public float InvisibleGlobalCoolDownTime { get; set; }

        public float SqrDistanceFromCurrentTarget { get; set; }

        public CastingBarDisplay CastingBarDisplay { get; private set; }

        public CharacterActions CharacterActions { get; private set; }

        public Statistics Stats { get; private set; }

        public void Initialize(in InitializationContext context)
        {
            ActionToTake = context.actionToTake;
            ActionBeingTaken = context.actionBeingTaken;
            IsCasting = context.isCasting;
            GlobalCoolDownTime = context.globalCoolDownTime;
            VisibleGlobalCoolDownTime = context.visibleGlobalCoolDownTime;
            InvisibleGlobalCoolDownTime = context.invisibleGlobalCoolDownTime;
            SqrDistanceFromCurrentTarget = context.sqrDistanceFromCurrentTarget;
            CastingBarDisplay = context.castingBarDisplay;
            CharacterActions = context.characterActions;
            Stats = context.stats;
        }

        public void SetCurrentTarget(GameObject gO)
        {
            CurrentTarget = gO;
        }

        public void SetRecentTarget(GameObject gO)
        {
            RecentTarget = gO;
        }
    }
}