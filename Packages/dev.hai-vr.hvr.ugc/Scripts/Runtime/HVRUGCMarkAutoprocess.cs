using UnityEngine;
using Object = UnityEngine.Object;

namespace HVR.UGC
{
    [AddComponentMenu("/")]
    [DefaultExecutionOrder(-100_000)]
    public class HVRUGCMarkAutoprocess : MonoBehaviour
#if HVR_IS_INSTALLED_IN_A_VRC_PROJECT
        , VRC.SDKBase.IEditorOnly
#endif
    {
        public bool applyOnPlay = false;
        
        private void Awake()
        {
            if (!applyOnPlay)
            {
                HVRUGCLogging.Log(this, "Deleting ourselves because we entered Play Mode. This should prevent NDMF from processing us, even if NDMF's Apply On Play is enabled.");
                Object.DestroyImmediate(this);
            }
            else
            {
                HVRUGCLogging.Log(this, $"ApplyOnPlay is enabled on {gameObject.name}, so this will not be deleted. NDMF may be imminently able to process this avatar as Play Mode is entered.");
            }
        }
    }
}