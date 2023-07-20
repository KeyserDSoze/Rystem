namespace System.Collections.Concurrent
{
    public sealed class ConcurrentList<T> : IList<T>
    {
        private readonly IList<T> _list = new List<T>();
        private readonly object _trafficLight = new();
        public T this[int index] { get => _list[index]; set => _list[index] = value; }
        public int Count => _list.Count;
        public bool IsReadOnly => _list.IsReadOnly;
        public void Add(T item)
        {
            lock (_trafficLight)
                _list.Add(item);
        }

        public void Clear()
        {
            lock (_trafficLight)
                _list.Clear();
        }

        public bool Contains(T item)
        {
            lock (_trafficLight)
                return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_trafficLight)
                _list.CopyTo(array, arrayIndex);
        }
        public IEnumerator<T> GetEnumerator()
            => _list.GetEnumerator();
        public int IndexOf(T item)
        {
            lock (_trafficLight)
                return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            lock (_trafficLight)
                _list.Insert(index, item);
        }
        public bool Remove(T item)
        {
            lock (_trafficLight)
                return _list.Remove(item);
        }
        public void RemoveAt(int index)
        {
            lock (_trafficLight)
                _list.RemoveAt(index);
        }
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
