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

    // 장면을 다시 불러오면(다시 하기) 장면에 배치된 새 객체가 쓰이도록 파괴된 객체를 잊는다.
    protected virtual void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    // 객체를 새로 만들지 않고, 있을 때만 돌려준다(OnDestroy 등 장면 정리 중에 쓴다).
    public static T ExistingInstance => instance;

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

                instance = FindAnyObjectByType<T>(); // 장면에 배치된 T 클래스 객체가 있으면 그 객체를 가리키게 한다.

                if (instance == null) // 장면에 T 클래스 객체가 없으면 새로 만든다.
                {
                    GameObject sgtObj = new GameObject();
                    instance = sgtObj.AddComponent<T>();
                    sgtObj.name = "(Sgt) " + typeof(T).Name; // 이름을 변경한다.
                }
                // 참고: 이 게임은 장면이 하나뿐이므로 DontDestroyOnLoad를 쓰지 않는다.
                // 쓰면 다시 하기로 장면을 불러올 때 이전 장면의 관리자(낡은 상태)가 남는다.

                return instance;
            }
        }
    }
}

// 주의: T myInstance = new T(); 같은 '비싱글턴(non-singleton) 생성자 사용'을 막으려면 싱글턴 클래스에 protected T() {} 같은 줄을 추가하여야 한다.
