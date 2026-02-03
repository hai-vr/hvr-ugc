using System.IO;
using UnityEditor;
using UnityEngine;

namespace HVR.UGC.Editor
{
    [CustomEditor(typeof(HVRUGCBelonging))]
    public class HVRUGCBelongingEditor : UnityEditor.Editor
    {
        private const string BuildBelongingFile = "Build belonging file";
        private const string MsgNoBelongingName = "Please set the belonging name before building.";
        
        private HVRChecker _checker;

        private void Awake()
        {
            _checker = new HVRChecker((MonoBehaviour)target, HVRBundleType.Belonging);
        }
        
        public override void OnInspectorGUI()
        {
            var my = (HVRUGCBelonging)target;
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HVRUGCBelonging.belongingName)));
            EditorGUILayout.Separator();
            
            var canBuild = !string.IsNullOrEmpty(my.belongingName) && !Application.isPlaying;
            EditorGUI.BeginDisabledGroup(!canBuild);
            if (GUILayout.Button(BuildBelongingFile, GUILayout.Height(HVREditorUtil.TallButtonHeight)))
            {
                HVRUGCLogging.Log(this, $"User has requested to build asset bundle for belonging {my.belongingName} (called {my.name} in the scene).");
                
                var assetBundleName = HVRUGCUtil.SanitizeAssetBundleFileName(my.belongingName);
                
                var bundlePath = HVRAssetBundleBuilder.PrepareAndBuildAssetBundle(my.gameObject, "UGC", assetBundleName, HVRBundleType.Belonging, my.bypassRestrictions);
                
                var directory = Path.GetDirectoryName(bundlePath);
                var newFileName = $"{assetBundleName}.belonging.bundle";
                var newPath = Path.Combine(directory, newFileName);
                if (File.Exists(newPath)) File.Delete(newPath);
                File.Move(bundlePath, newPath);
                
                HVRUGCLogging.Log(this, $"Successfully built asset bundle at {newPath}");
                EditorUtility.RevealInFinder(newPath);
            }
            EditorGUI.EndDisabledGroup();
            
            if (my.belongingName == null)
            {
                EditorGUILayout.HelpBox(MsgNoBelongingName, MessageType.Warning);
            }
            
            _checker.EditorLayout(my.bypassRestrictions);

            serializedObject.ApplyModifiedProperties();
        }
    }
}