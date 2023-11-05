using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : Singleton<ObjectPoolManager>
{
    private ObjectPoolManager() { }

   [System.Serializable] // 클래스, 구조체, 열거형, 대리자에 붙일 수 있는 특성(인스펙터에 표시 가능)
    public class ObjectPoolInfo
    {
        public string name;
        public GameObject prefab;
        public int size;
    }

    public List<ObjectPoolInfo> objectPoolInfoList;
    public Dictionary<string, Queue<GameObject>> objectPoolDictionary;

    void Start()
    {
        objectPoolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (ObjectPoolInfo objPoolInfo in objectPoolInfoList)
        {
            Queue<GameObject> objPool = new Queue<GameObject>();

            for (int i = 0; i < objPoolInfo.size; i++)
            {
                GameObject obj = Instantiate(objPoolInfo.prefab);
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

        Queue<GameObject> objQueue = objectPoolDictionary[objPoolName];

        GameObject objToSpawn;

        if (objQueue.Peek().activeSelf)
        {
            objToSpawn = Instantiate(objQueue.Peek().gameObject);
            objToSpawn.SetActive(false);
        }
        else
        {
            objToSpawn = objQueue.Dequeue(); // 큐 선두에 있는 오브젝트를 꺼낸다.
        }

        if (shouldBeEnabledBeforeReturn) objToSpawn.SetActive(true);
        objToSpawn.transform.position = pos;
        objToSpawn.transform.rotation = rot;

        IPoolable pooledObj = objToSpawn.GetComponent<IPoolable>();

        if (!(pooledObj is null))
        {
            pooledObj.OnObjectSpawn();
        }

        objQueue.Enqueue(objToSpawn); // 꺼냈던 오브젝트를 큐 후미에 넣는다.

        return objToSpawn;
    }
}
