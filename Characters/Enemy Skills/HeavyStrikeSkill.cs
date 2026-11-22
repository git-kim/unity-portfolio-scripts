using System.Collections;
using Characters.Handlers;
using Characters.StatisticsScripts;
using Managers;
using UnityEngine;

namespace Characters.EnemySkills
{
    /// <summary>
    /// Skeleton skill: a short wind-up, then a heavy blow that lands only if the target is still in reach.
    /// </summary>
    public class HeavyStrikeSkill : EnemySkill
    {
        [SerializeField] private float windUpTime = 0.7f;
        [SerializeField] private float reach = 3.5f;
        [SerializeField] private float damageMultiplier = 1.8f;
        [SerializeField] private float recoveryTime = 0.8f;

        protected override IEnumerator Execute(Transform target, StatChangeHandler targetStats)
        {
            Animator.SetInteger("MovementMode", 0);
            Animator.SetTrigger(SkillTrigger);

            yield return new WaitForSeconds(windUpTime);
            AudioManager.Instance.Play(Sfx.HeavyStrike, Owner.transform.position);

            if (target && SqrHorizontalDistance(target.position, Owner.transform.position) <= reach * reach)
            {
                var power = Mathf.RoundToInt(Owner.Stats[Stat.MeleeAttack] * damageMultiplier);
                Owner.DealDamage(targetStats, power, true, skillName);
            }

            yield return new WaitForSeconds(recoveryTime);
        }
    }
}
