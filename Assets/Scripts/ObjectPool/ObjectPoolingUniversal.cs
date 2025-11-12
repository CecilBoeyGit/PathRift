using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolingUniversal : MonoBehaviour
{
    [Header("Pool Setup")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private int initialCapacity = 20;
    [SerializeField] private bool allowExpand = true;
    [SerializeField] private Transform storageParent;

    private readonly Queue<Transform> _pool = new Queue<Transform>();
    private bool _initialized;

    void Awake()
    {
        Prewarm();
    }

    public void Prewarm()
    {
        if (_initialized || prefab == null) return;
        _initialized = true;

        if (storageParent == null)
        {
            storageParent = transform;
        }

        for (int i = 0; i < initialCapacity; i++)
        {
            var t = CreateInstance();
            t.SetParent(storageParent, false);
            Return(t);
        }
    }

    private Transform CreateInstance()
    {
        var go = Instantiate(prefab);
        return go.transform;
    }

    /// Spawn a pooled object at a world position (identity rotation).
    public Transform SpawnAt(Vector3 worldPos)
    {
        return SetPoolObject(worldPos, Quaternion.identity);
    }

    /// Spawn at position/rotation, optionally under a parent (keeps world transform).
    public Transform SetPoolObject(Vector3 worldPos, Quaternion worldRot)
    {
        if (!_initialized) Prewarm();

        Transform t = _pool.Count > 0 ? _pool.Dequeue() :
                      allowExpand ? CreateInstance() : null;

        if (t == null) return null;

        t.position = worldPos;
        t.rotation = worldRot;
        t.gameObject.SetActive(true);
        return t;
    }

    /// Return an instance to the pool.
    public void Return(Transform t)
    {
        if (t == null) return;
        t.gameObject.SetActive(false);
        _pool.Enqueue(t);
    }
}

