using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireballSpawner : MonoBehaviour
{
    ObjectPoolManager OBJP;
    Fireball spawnedFireball;
    Transform trnsfrm;

    void Awake()
    {
        OBJP = ObjectPoolManager.Instance;
        trnsfrm = gameObject.transform;
    }

    public void SpawnFireball(GameObject target, int magicDamage, in string actionName)
    {
        GameObject spawnedObject = OBJP.SpawnObjectFromPool("Fireball", trnsfrm.position, Quaternion.identity);
        spawnedFireball = spawnedObject.GetComponent<Fireball>();
        spawnedFireball.SetTarget(target);
        spawnedFireball.Fire(magicDamage, in actionName);
    }
}
