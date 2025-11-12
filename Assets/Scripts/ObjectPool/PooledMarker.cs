using UnityEngine;

public class PooledMarker : MonoBehaviour
{
    public bool moving;
    public bool stationary;
    public ObjectPoolingUniversal ownerPool;

    public void Return()
    {
        if (ownerPool != null) ownerPool.Return(transform);
    }
}

