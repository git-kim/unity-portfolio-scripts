using System.Collections.Generic;
using UnityEngine;

namespace ObserverPattern
{
    public abstract class Subject: MonoBehaviour
    {
        private readonly List<IObserver> observers = new List<IObserver>();

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
            foreach (var observer in observers)
                observer.React();
        }

        protected void Notify2()
        {
            foreach (var observer in observers)
                observer.React2();
        }
    }
}