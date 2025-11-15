using UnityEngine;


namespace Niantic.Lightship.MetaQuest.InternalSamples
{
    public class PooledMarker : MonoBehaviour
    {
        public bool moving;
        public bool stationary;
        public ObjectPoolingUniversal ownerPool;
        public ObjectDetectionFiltered DetectionResultsManager;

        private void OnDisable()
        {
            Return();
            if (moving)
            {
                int index = DetectionResultsManager._moving.IndexOf(this);
                DetectionResultsManager._moving.RemoveAt(index);
            }
            else if (stationary)
            {
                int index = DetectionResultsManager._stationary.IndexOf(this);
                DetectionResultsManager._stationary.RemoveAt(index);
            }
        }

        public void Return()
        {
            if (ownerPool != null) ownerPool.Return(transform);
        }
    }
}