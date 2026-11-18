using System.Collections.Generic;
using UnityEngine;

namespace Shrunk
{
    /// <summary>Reuses instances of one prefab instead of creating and destroying them (cheap on phones).</summary>
    public class Pool<T> where T : Component
    {
        readonly T _prefab;
        readonly Transform _parent;
        readonly Stack<T> _free = new Stack<T>();

        public Pool(T prefab, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;
        }

        /// <summary>Returns an inactive instance. Position it, then activate it.</summary>
        public T Get()
        {
            while (_free.Count > 0)
            {
                var item = _free.Pop();
                if (item != null) return item;
            }

            var created = Object.Instantiate(_prefab, _parent);
            created.gameObject.SetActive(false);
            return created;
        }

        public void Release(T item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            _free.Push(item);
        }
    }
}
