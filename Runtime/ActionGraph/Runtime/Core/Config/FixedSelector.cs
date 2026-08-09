public class FixedSelector<T> : IDataSelector<T>
{
    private T _value;

    public FixedSelector(T value)
    {
        _value = value;
    }

    public T GetNext() => _value;
    
    public void Reset() { }
}