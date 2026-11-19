using System.Collections;
using System.Linq;
using Characters.Handlers;
using Managers;
using UnityEngine;

namespace Characters.EnemySkills
{
    /// <summary>
    /// Boss skill: channels for <see cref="castTime"/> seconds while a ground marker fills up,
    /// then hits every player still inside the circle. Players dodge it by leaving the circle.
    /// </summary>
    public class AoeCastSkill : EnemySkill
    {
        [SerializeField] private float castTime = 3f;
        [SerializeField] private float radius = 7f;
        [SerializeField] private int magicPower = 1300;
        [SerializeField] private float recoveryTime = 1f;
        [SerializeField] private Material telegraphMaterial;
        [SerializeField] private Color telegraphColor = new Color(0.55f, 0.1f, 0.85f, 0.45f);
        [SerializeField] private GameObject chargeEffectPrefab;
        [SerializeField] private GameObject releaseEffectPrefab;

        private AoeTelegraph telegraph;
        private GameObject chargeEffect;

        public float Radius => radius;

        protected override IEnumerator Execute(Transform target, StatChangeHandler targetStats)
        {
            var center = Owner.transform.position;

            Animator.SetBool(CastBool, true);
            telegraph = AoeTelegraph.Create(center, radius, telegraphMaterial, telegraphColor);
            if (chargeEffectPrefab)
                chargeEffect = Instantiate(chargeEffectPrefab, Owner.transform.position, Quaternion.identity, Owner.transform);
            if (Owner.CastingBar)
                Owner.CastingBar.Show(skillName, castTime);

            for (var elapsed = 0f; elapsed < castTime; elapsed += Time.deltaTime)
            {
                telegraph.Progress = elapsed / castTime;
                yield return null;
            }

            Animator.SetBool(CastBool, false);
            Animator.SetTrigger(SkillTrigger);
            if (releaseEffectPrefab)
                Destroy(Instantiate(releaseEffectPrefab, center, Quaternion.identity), 5f);

            // Copy first: a player killed by the blast removes itself from PlayersAlive.
            foreach (var player in GameManager.Instance.PlayersAlive.Values.ToList())
            {
                if (SqrHorizontalDistance(player.position, center) > radius * radius)
                    continue;
                Owner.DealDamage(player.GetComponent<StatChangeHandler>(), magicPower, false, skillName);
            }

            CleanUp();
            yield return new WaitForSeconds(recoveryTime);
        }

        protected override void CleanUp()
        {
            if (Animator)
                Animator.SetBool(CastBool, false);
            if (telegraph)
                Destroy(telegraph.gameObject);
            if (chargeEffect)
                Destroy(chargeEffect);
            if (Owner && Owner.CastingBar)
                Owner.CastingBar.StopShowingCastingBar();
            telegraph = null;
            chargeEffect = null;
        }
    }
}
