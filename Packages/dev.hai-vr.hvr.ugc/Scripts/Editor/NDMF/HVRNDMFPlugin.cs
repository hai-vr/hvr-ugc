#if HVR_NDMF
using HVR.UGC.Editor;
using nadena.dev.ndmf;
using UnityEngine;
using Object = UnityEngine.Object;

[assembly: ExportsPlugin(typeof(HVRNDMFPlugin))]
namespace HVR.UGC.Editor
{
    // Read below for an explanation of why this plugin is marked as RunsOnAllPlatforms despite being HVR-specific.
    [RunsOnAllPlatforms]
    public class HVRNDMFPlugin : Plugin<HVRNDMFPlugin>
    {
        public override string QualifiedName => "dev.hai-vr.hvr.HVRNDMFPlugin";
        public override string DisplayName => "HVRNDMFPlugin";
        
        protected override void Configure()
        {
#if HVR_HAS_JIGGLEPHYSICS
            // NOTE TO MAINTAINER:
            // This will only run in the NDMF Console is set to run on the platform HVR, not Generic Avatar.
            // That even seems to apply if Apply On Play was triggered as a result of our HVR-specific component HVRUGC Mark Autoprocess being attached to the avatar.
            // That's why 
            InPhase(BuildPhase.PlatformFinish).Run("Check JiggleRig can run in Play Mode", context =>
            {
                if (!Application.isPlaying) return;
                
                var isAvatarNotFromHVR = context.AvatarRootTransform.GetComponent<HVRUGCMarkAutoprocess>() == null;
                if (isAvatarNotFromHVR) return;
                
                var thereIsNoJiggleRig = context.AvatarRootTransform.GetComponentsInChildren<GatorDragonGames.JigglePhysics.JiggleRig>(true).Length == 0;
                if (thereIsNoJiggleRig) return;
                
                var jiggleUpdateExists = Object.FindAnyObjectByType<GatorDragonGames.JigglePhysics.JiggleUpdateExample>(FindObjectsInactive.Include) != null;
                if (jiggleUpdateExists) return;

                var initTypeExists = HVREditorUtil.TryGetTypeByFullName(HVRUGCHardcodedTypeNames.HVRInitializer, out var initializerType);
                if (initTypeExists && Object.FindAnyObjectByType(initializerType, FindObjectsInactive.Include) != null) return;
                
                var jiggleUpdateTypeExists = HVREditorUtil.TryGetTypeByFullName(HVRUGCHardcodedTypeNames.HVRJiggleUpdate, out var jiggleUpdateType);
                if (jiggleUpdateTypeExists && Object.FindAnyObjectByType(jiggleUpdateType, FindObjectsInactive.Include) != null) return;

                HVRUGCLogging.Log(this, "JiggleRig might not be able to run in Play Mode, as there is no HVRSystems initialized in the scene, and also there is no JiggleUpdateExample in the scene. We will create one.");
                HVRUGCUtil.UGC_NewDisabledObjectWithComponent<GatorDragonGames.JigglePhysics.JiggleUpdateExample>("HVRNDMF-JiggleRigHelper", null)
                    .gameObject.SetActive(true);
            });
#endif
        }
    }
}
#endif