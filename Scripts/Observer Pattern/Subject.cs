using System.Collections.Generic;
using UnityEngine;

namespace ObserverPattern
{
    abstract public class Subject: MonoBehaviour
    {
        readonly List<IObserver> observers = new List<IObserver>();
        // 주의: readonly는 const와는 다르게 '런타임'에 값을 확정하게 하는 키워드이므로 동적 생성에 const는 사용할 수 없다.
        // 참고: readonly가 있더라도 리스트에는 요소 추가, 요소 제거 등이 가능하다.
        // 참고: readonly 변수는 선언할 때가 아니면 생성자 안에서만 변수를 초기화할 수 있다.

        protected void AddObserver(IObserver observer)
        {
            observers.Add(observer);
        }

        protected void RemoveObserver(IObserver observer)
        {
            observers.Remove(observer);
        }

        protected void Notify()
        {
            foreach (IObserver observer in observers)
                observer.React();
        }

        protected void Notify2()
        {
            foreach (IObserver observer in observers)
                observer.React2();
        }
    }
}
