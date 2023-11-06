namespace ObserverPattern
{
    public interface IObserver
    {
        // Subject에서 Notify가 호출되면 호출되는 함수
        void React();

        void React2();
    }
}