public interface IDataSelector
{
    void Reset();
}

public interface IDataSelector<out T> : IDataSelector
{
    T GetNext();
}