namespace Utilities;

public static class GameObjectPoolParent
{
    private static Transform _commonParent;

    public static Transform CommonParent
    {
        get
        {
            if (!_commonParent) _commonParent = new GameObject("Common Pools Parent").transform;
            return _commonParent;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _commonParent = null;
}

public sealed class GameObjectPool<T> where T : MonoBehaviour, IResetable
{
    private readonly HashSet<T> _inPool = new();
    private readonly List<T> _items = new();
    private readonly Transform _parent;
    private readonly T _prefab;

    public GameObjectPool(T prefab, Transform parent = null)
    {
        _prefab = prefab;
        _parent = parent ? parent : GameObjectPoolParent.CommonParent;
    }

    public T Get()
    {
        if (_items.Count <= 0)
            return _prefab ? Object.Instantiate(_prefab, _parent) : null;
        var last = _items.Count - 1;
        var item = _items[last];
        _items.RemoveAt(last);
        _inPool.Remove(item);
        return item;
    }

    public void Return(T item)
    {
        if (!_inPool.Add(item)) return;
        item.ResetView();
        item.transform.SetParent(_parent, false);
        _items.Add(item);
    }

    public void DestroyAll()
    {
        for (var i = 0; i < _items.Count; i++)
            if (_items[i])
                Object.Destroy(_items[i].gameObject);
        _items.Clear();
        _inPool.Clear();
    }

    public void Prewarm(int count)
    {
        for (var i = _items.Count; i < count; i++)
        {
            var item = Object.Instantiate(_prefab, _parent);
            item.ResetView();
            _items.Add(item);
            _inPool.Add(item);
        }
    }
}