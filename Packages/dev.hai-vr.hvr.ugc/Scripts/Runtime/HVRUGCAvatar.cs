using UnityEngine;

namespace HVR.UGC
{
    /// Represents a humanoid avatar, which may be worn by a person or used as an NPC.
    [AddComponentMenu("HVR/UGC/HVRUGC Avatar")]
    public class HVRUGCAvatar : MonoBehaviour
#if HVR_IS_INSTALLED_IN_A_VRC_PROJECT
        , VRC.SDKBase.IEditorOnly
#endif
    {
        public string avatarName;
        public string avatarBase;
        public Transform viewpoint;
        public float floor;

        public bool bypassRestrictions; // For debug purposes. If true, this does not remove components from the object.

        public SkinnedMeshRenderer[] collisionMeshes = new SkinnedMeshRenderer[0];
        
        public event FullyInitialized OnFullyInitialized;
        public delegate void FullyInitialized(HVRUGCAvatar avatar);
        public void TriggerFullyInitialized() => OnFullyInitialized?.Invoke(this);

        private void Awake()
        {
            // This is a UGC component, so the fields should not be trusted.
            collisionMeshes = HVRUGCUtil.SlowSanitizeEndUserProvidedObjectArray(collisionMeshes);
            
            var animator = GetComponent<Animator>();
            if (viewpoint != null && animator != null)
            {
                var headTransform = animator.GetBoneTransform(HumanBodyBones.Head);
                if (viewpoint.transform != headTransform)
                {
                    viewpoint.SetParent(headTransform, true);
                }
            }
        }
        
#if UNITY_EDITOR
        private GUIStyle _style;
        private void OnDrawGizmosSelected()
        {
            if (viewpoint != null)
            {
                if (_style == null)
                {
                    _style = new GUIStyle();
                    _style.normal.textColor = Color.blue;
                }
                
                var pos = viewpoint.position;
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(pos, 0.005f);
                UnityEditor.Handles.Label(pos, "Viewpoint", _style);
            }
        }
#endif
        
        public float EyeHeight()
        {
            var bottom = floor;
            
            float top;
            if (viewpoint != null)
            {
                top = viewpoint.position.y;
            }
            else
            {
                var animator = GetComponent<Animator>();
                if (animator == null || animator.avatar == null) return 1f - bottom;
                
                top = animator.GetBoneTransform(HumanBodyBones.LeftEye).position.y;
            }

            return top - bottom;
        }
    }
}