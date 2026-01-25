using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if HVR_NDMF
using nadena.dev.ndmf;
#endif

namespace HVR.UGC.Editor
{
    public class HVRAssetBundleBuilder
    {
        public static string PrepareAndBuildAssetBundle(GameObject original, string prefabGameObjectName, string assetBundleName, bool doNotSanitizeComponents)
        {
            if (assetBundleName != HVRUGCUtil.SanitizeAssetBundleFileName(assetBundleName))
            {
                throw new InvalidOperationException("Asset bundle file name was incorrectly sanitized.");
            }
            
            GameObject copy = null;
            var tempPrefabPath = $"Assets/{prefabGameObjectName}.prefab";
            try
            {
                copy = Object.Instantiate(original).gameObject;
                copy.SetActive(true);

#if HVR_NDMF
                AvatarProcessor.ProcessAvatar(copy, HVRPlatform.Instance);
#endif

                if (!doNotSanitizeComponents)
                {
                    HVRAssetBundleSanitization.PreInstantiateSanitize(copy);
                }
                Scrub(copy);
                
                // TODO: This does nothing for now.
                HVRPrecalculated.PrecalculateCollisionMeshes(copy);
                
                PrefabUtility.SaveAsPrefabAsset(copy, tempPrefabPath);

                return CreateAssetBundle(tempPrefabPath, assetBundleName);
            }
            finally
            {
                if (copy != null) Object.DestroyImmediate(copy);
                AssetDatabase.DeleteAsset(tempPrefabPath);
            }
        }

        private static string CreateAssetBundle(string tempPrefabPath, string assetBundleFileName)
        {
            var builds = new AssetBundleBuild[1];
            builds[0].assetBundleName = assetBundleFileName;
            builds[0].assetNames = new[] { tempPrefabPath };

            if (!Directory.Exists("AssetBundles"))
            {
                Directory.CreateDirectory("AssetBundles");
            }

            BuildPipeline.BuildAssetBundles("AssetBundles", builds, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
            AssetDatabase.Refresh();
            
            var bundlePath = $"AssetBundles/{assetBundleFileName}";
            if (File.Exists(bundlePath))
            {
                var manifestPath = $"AssetBundles/{assetBundleFileName}.manifest";
                if (File.Exists(manifestPath))
                {
                    File.Delete(manifestPath);
                }
                
                return bundlePath;
            }
            else
            {
                HVRUGCLogging.LogError(typeof(HVRAssetBundleBuilder), "Asset bundle build failed.");
                throw new InvalidOperationException("Asset bundle build failed.");
            }
        }

        private static void Scrub(GameObject copy)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(copy.gameObject);
            
            var copyTransform = copy.transform;
            var transforms = Enumerable.Range(0, copyTransform.childCount)
                .Select(i => copyTransform.GetChild(i))
                .ToList();
            
            foreach (var t in transforms)
            {
                DestroyIfEditorOnlyRecursive(t.gameObject);
            }
        }

        private static void DestroyIfEditorOnlyRecursive(GameObject subject)
        {
            if (subject.CompareTag("EditorOnly"))
            {
                Object.DestroyImmediate(subject);
            }
            else
            {
                foreach (Transform child in subject.transform)
                {
                    DestroyIfEditorOnlyRecursive(child.gameObject);
                }
            }
        }
    }
}