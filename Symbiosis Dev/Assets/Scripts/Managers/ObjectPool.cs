// Assets/Scripts/Systems/ObjectPool.cs
using UnityEngine;
using System.Collections.Generic;

public class ObjectPool<T> where T : MonoBehaviour
{
    private T prefab;
    private Transform parent;
    private Queue<T> pool = new Queue<T>();
    private int initialSize;

    public ObjectPool(T prefab, Transform parent, int initialSize)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.initialSize = initialSize;

        for (int i = 0; i < initialSize; i++)
        {
            T obj = GameObject.Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public T GetFromPool()
    {
        if (pool.Count > 0)
        {
            T obj = pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }
        else
        {
            // Optionally expand the pool
            T obj = GameObject.Instantiate(prefab, parent);
            return obj;
        }
    }

    public void ReturnToPool(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
