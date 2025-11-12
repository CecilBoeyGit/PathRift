using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolingUniversal : MonoBehaviour
{
    [SerializeField]
    private GameObject _PooledObjectPrefab;

    private List<int> _openIndices = new List<int>();
    private List<GameObject> _PooledObjects = new List<GameObject>();

    public void CreateRect(Vector3 worldPos)
    {
        if (_openIndices.Count == 0)
        {
            var newObj = Instantiate(_PooledObjectPrefab, parent: this.transform);

            _PooledObjects.Add(newObj);
            _openIndices.Add(_PooledObjects.Count - 1);
        }

        // Treat the first index as a queue
        int index = _openIndices[0];
        _openIndices.RemoveAt(0);

        GameObject rectangle = _PooledObjects[index];
        rectangle.gameObject.transform.position = worldPos;
        rectangle.gameObject.SetActive(true);
    }

    public void ClearRects()
    {
        for (var i = 0; i < _PooledObjects.Count; i++)
        {
            _PooledObjects[i].gameObject.SetActive(false);
            _openIndices.Add(i);
        }
    }
}

