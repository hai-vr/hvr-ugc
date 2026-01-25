#if HVR_NDMF
using System;
using nadena.dev.ndmf.platform;
using UnityEngine;

namespace HVR.UGC.Editor
{
    [NDMFPlatformProvider]
    public class HVRPlatform : INDMFPlatformProvider
    {
        public static readonly INDMFPlatformProvider Instance = new HVRPlatform();

        public string QualifiedName => "dev.hai-vr.hvr";
        public string DisplayName => "HVR";
        public Texture2D? Icon => null;
        // We aren't using HVRUGCAvatar because NDMF's "Apply on Play" conflict with the normal function of the app.
        public Type AvatarRootComponentType => typeof(HVRUGCMarkAutoprocess);
        public bool HasNativeConfigData => true;

        public BuildUIElement? CreateBuildUI()
        {
            return new HVRPlatformBuildUI();
        }

        public CommonAvatarInfo ExtractCommonAvatarInfo(GameObject avatarRoot)
        {
            var ugcAcatar = avatarRoot.GetComponent<HVRUGCAvatar>();

            var cai = new CommonAvatarInfo();
            cai.EyePosition = avatarRoot.transform.InverseTransformVector(
                ugcAcatar.viewpoint.position
            );

            return cai;
        }

        public void InitFromCommonAvatarInfo(GameObject avatarRoot, CommonAvatarInfo cai)
        {
            // TODO
        }

        public bool CanInitFromCommonAvatarInfo(GameObject avatarRoot, CommonAvatarInfo info)
        {
            // TODO
            return true;
        }
    }

    public class HVRPlatformBuildUI : BuildUIElement
    {
    }
}
#endif