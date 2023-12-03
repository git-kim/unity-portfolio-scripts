using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class NonPooledParticleEffectManager : Singleton<NonPooledParticleEffectManager>
    {
        private NonPooledParticleEffectManager() { }

        [Serializable]
        public class ParticleEffectInfo
        {
            public ParticleEffectName name;
            public GameObject prefab;
            [HideInInspector] public ParticleSystem prefabParticleSystem;
            [HideInInspector] public Transform prefabTransform;
        }

        private IEnumerator DisableAfterWaiting(float timeInSeconds, ParticleEffectInfo particleEffect)
        {
            yield return new WaitForSeconds(timeInSeconds);
            particleEffect.prefabTransform.SetParent(gameObject.transform, false);
            particleEffect.prefab.SetActive(false);
        }

        public List<ParticleEffectInfo> particleEffects = new List<ParticleEffectInfo>();

        private void Awake()
        {
            foreach (var particleEffect in particleEffects)
            {
                var prefab = Instantiate(particleEffect.prefab); // 참고: 프리팹을 실체화하여야 SetParent를 호출할 수 있다.
                particleEffect.prefab = prefab;
                particleEffect.prefabParticleSystem = prefab.GetComponent<ParticleSystem>();
                particleEffect.prefabTransform = prefab.transform;
                prefab.SetActive(false);
                prefab.transform.SetParent(gameObject.transform, false);
            }
        }

        public void PlayParticleEffect(ParticleEffectName name, Transform targetTransform, Vector3 localPosition, Vector3 toDirection, Vector3 localScale, float duration, bool shouldFollowTarget)
        {
            var index = particleEffects.FindIndex(effect => effect.name == name);
            var effectToPlay = particleEffects[index];

            if (shouldFollowTarget)
                effectToPlay.prefabTransform.SetParent(targetTransform, false);

            effectToPlay.prefabTransform.localPosition = localPosition;

            if (toDirection != Vector3.zero)
                effectToPlay.prefabTransform.rotation = Quaternion.FromToRotation(effectToPlay.prefabTransform.rotation.eulerAngles, toDirection);

            effectToPlay.prefabTransform.localScale = localScale;


            effectToPlay.prefabParticleSystem.Simulate(0.0f, true, true);
            effectToPlay.prefab.SetActive(true);
            effectToPlay.prefabParticleSystem.Play();

            StartCoroutine(DisableAfterWaiting(duration, effectToPlay));
        }
    }
}