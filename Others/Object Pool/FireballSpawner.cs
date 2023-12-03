using Managers;
using UnityEngine;

namespace ObjectPool
{
    public class FireballSpawner : MonoBehaviour
    {
        private ObjectPoolManager objectPoolManagerInstance;
        private Fireball spawnedFireball;
        private Transform thisTransform;

        void Awake()
        {
            objectPoolManagerInstance = ObjectPoolManager.Instance;
            thisTransform = gameObject.transform;
        }

        public void SpawnFireball(GameObject target, int magicDamage, in string actionName)
        {
            GameObject spawnedObject = objectPoolManagerInstance.SpawnObjectFromPool("Fireball", thisTransform.position, Quaternion.identity);
            spawnedFireball = spawnedObject.GetComponent<Fireball>();
            spawnedFireball.SetTarget(target);
            spawnedFireball.Fire(magicDamage, in actionName);
        }
    }
}