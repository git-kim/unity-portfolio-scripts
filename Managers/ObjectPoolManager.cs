using ObjectPool;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class ObjectPoolManager : Singleton<ObjectPoolManager>
    {
        private ObjectPoolManager() { }

        [Serializable]
        public class ObjectPoolInfo
        {
            public string name;
            public GameObject prefab;
            public int size;
        }

        public List<ObjectPoolInfo> objectPoolInfoList;
        private Dictionary<string, Queue<GameObject>> objectPoolDictionary;

        private void Start()
        {
            objectPoolDictionary = new Dictionary<string, Queue<GameObject>>();

            foreach (var objPoolInfo in objectPoolInfoList)
            {
                var objPool = new Queue<GameObject>();

                for (var i = 0; i < objPoolInfo.size; i++)
                {
                    var obj = Instantiate(objPoolInfo.prefab);
                    obj.SetActive(false);
                    objPool.Enqueue(obj);
                }

                objectPoolDictionary.Add(objPoolInfo.name, objPool);
            }
        }

        public GameObject SpawnObjectFromPool(string objPoolName, Vector3 pos, Quaternion rot, bool shouldBeEnabledBeforeReturn = true)
        {
            if (!objectPoolDictionary.ContainsKey(objPoolName))
            {
                Debug.LogError(objPoolName + " (오브젝트 풀) 부재");
                return null;
            }

            var objQueue = objectPoolDictionary[objPoolName];

            GameObject objToSpawn;

            if (objQueue.Peek().activeSelf)
            {
                objToSpawn = Instantiate(objQueue.Peek());
                objToSpawn.SetActive(false);
            }
            else
            {
                objToSpawn = objQueue.Dequeue();
            }

            if (shouldBeEnabledBeforeReturn) objToSpawn.SetActive(true);
            objToSpawn.transform.SetPositionAndRotation(pos, rot);

            if (objToSpawn.TryGetComponent<IPoolable>(out var pooledObj))
            {
                pooledObj.OnObjectSpawn();
            }

            objQueue.Enqueue(objToSpawn);

            return objToSpawn;
        }
    }
}