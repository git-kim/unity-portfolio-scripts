using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance = null; // T 클래스 객체를 가리킬 변수를 선언한다.
    private static readonly object ObjForLock = new object(); // 오브젝트형 변수를 생성한다.

    private static bool isAppShuttingDown = false; // 프로그램이 종료 중인지를 저장할 변수를 선언하고 초기화한다.

    void OnApplicationQuit() // MonoBehaviour.OnApplicationQuit(): Sent to all GameObjects before the application quits.
    {
        isAppShuttingDown = true; // 프로그램이 종료 중임을 나타내도록 변수 값을 변경한다.
    }

    // 프로퍼티 정의
    public static T Instance // 주의: 프로퍼티명에 대문자를 사용한다.
    {
        get
        {
            if (isAppShuttingDown) // 프로그램이 종료 중이면
            {
                return null;
            }

            lock (ObjForLock) // 특정 객체(objForLock) 대상 스레드 간 경쟁 접근 방지(참고: 잠금은 블록이 끝나야 풀린다.)
            {
                if (instance != null)
                    return instance;

                instance = FindObjectOfType<T>(); // 기존에 생성된 T 클래스 객체가 있는지 찾고 있다면 instance가 그 객체를 가리키게 한다.

                if (instance == null) // 기존에 생성된 T 클래스 객체가 없으면(즉 하이어라키에 해당 객체가 없으면)
                {
                    GameObject sgtObj = new GameObject(); // 또는 var sgtObj = new GameObject();
                    instance = sgtObj.AddComponent<T>();
                    sgtObj.name = "(Sgt) " + typeof(T).Name; // 이름을 변경한다.

                    DontDestroyOnLoad(sgtObj); // 장면 전환이 있더라도 해당 객체가 파괴되지 않게 한다.
                }
                //else if (FindObjectsOfType<T>().Length > 1) // T 클래스 객체 수가 1 이하인지 검사
                //{
                //    Debug.LogError("싱글턴 클래스 객체가 둘 이상입니다. 클래스명: " + typeof(T).Name + ".");
                //}
                else // 기존에 생성된 T 클래스 객체가 있으면(즉 하이어라키에 해당 객체가 있으면)
                {
                    instance.name = "(Sgt) " + instance.name; // 이름을 변경한다.
                    DontDestroyOnLoad(instance); // 장면 전환이 있더라도 해당 객체가 파괴되지 않게 한다.
                }
                return instance;
            }

        }
    }
}

// 주의: T myInstance = new T(); 같은 '비싱글턴(non-singleton) 생성자 사용'을 막으려면 싱글턴 클래스에 protected T() {} 같은 줄을 추가하여야 한다.