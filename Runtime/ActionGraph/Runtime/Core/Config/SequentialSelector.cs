using System;

public class SequentialSelector<T> : IDataSelector<T>
{
    private readonly T[] _data;
    private int _index;

    public SequentialSelector(T[] data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _index = 0;
    }

    public T GetNext()
    {
        if (_data.Length == 0)
            throw new Exception("Selector has no data");

        var value = _data[_index];
        _index = (_index + 1) % _data.Length;
        return value;
    }

    public void Reset()
    {
        _index = 0;
    }
}