using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ParticleEffectName
{
    None,
    SprintBuff,
    HealHP,
    HealMP,
    CurseDebuff
}

public class NonPooledParticleEffectManager : Singleton<NonPooledParticleEffectManager>
{
    private NonPooledParticleEffectManager() { }

    [System.Serializable] // 클래스, 구조체, 열거형, 대리자에 붙일 수 있는 특성(인스펙터에 표시 가능)
    public class ParticleEffectInfo
    {
        public ParticleEffectName name;
        public GameObject prefab;
        [HideInInspector] public ParticleSystem prefabParticleSystem;
        [HideInInspector] public Transform prefabTransform;
    }


    IEnumerator DisableAfterWaiting(float timeInSeconds, ParticleEffectInfo particleEffect)
    {
        yield return new WaitForSeconds(timeInSeconds);
        particleEffect.prefabTransform.SetParent(gameObject.transform, false);
        particleEffect.prefab.SetActive(false);
    }

    public List<ParticleEffectInfo> particleEffects = new List<ParticleEffectInfo>();

    void Awake()
    {
        foreach (ParticleEffectInfo particleEffect in particleEffects)
        {
            GameObject prefab = Instantiate(particleEffect.prefab); // 참고: 프리팹을 실체화하여야 SetParent를 호출할 수 있다.
            particleEffect.prefab = prefab;
            particleEffect.prefabParticleSystem = prefab.GetComponent<ParticleSystem>();
            particleEffect.prefabTransform = prefab.transform;
            prefab.SetActive(false);
            prefab.transform.SetParent(gameObject.transform, false);
        }
    }

    public void PlayParticleEffect(ParticleEffectName name, Transform targetTransform, Vector3 localPosition, Vector3 toDirection, Vector3 localScale, float duration, bool shouldFollowTarget)
    {
        int index = particleEffects.FindIndex(effect => effect.name == name);
        ParticleEffectInfo effectToPlay = particleEffects[index];

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
